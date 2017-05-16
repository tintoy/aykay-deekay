using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Configuration;
using AKDK.Actors;
using System;
using System.IO;
using System.Threading;

namespace AKDK.Examples.Orchestration
{
    using Actors;
    using Messages;

    /// <summary>
    ///     Example of using AK/DK to orchestrate Docker containers.
    /// </summary>
    static class Program
    {
        /// <summary>
        ///     Basic HOCON-style configuration for the test harness.
        /// </summary>
        static readonly Config AkkaConfig = ConfigurationFactory.ParseString(
            @"
                akka {
                    loglevel = DEBUG
                    stdout-loglevel = DEBUG
                    suppress-json-serializer-warning = on
                    loggers = [ ""Akka.Event.StandardOutLogger"" ]
                }
            "
        );

        /// <summary>
        ///     The main program entry-point.
        /// </summary>
        static void Main()
        {
            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );

            // All state data will be written to this directory.
            DirectoryInfo stateDirectory = new DirectoryInfo(Path.Combine(
                Directory.GetCurrentDirectory(), "_state"
            ));

            ManualResetEvent completed = new ManualResetEvent(initialState: false);
            try
            {
                // Match colour for INFO messages from Akka logger.
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine("Starting ...");

                using (ActorSystem system = ActorSystem.Create("orchestration-example", AkkaConfig))
                {
                    Console.WriteLine("Running.");

                    IActorRef app = system.ActorOf(actor =>
                    {
                        IActorRef client = null;
                        IActorRef jobStore = null;
                        IActorRef launcher = null;
                        IActorRef harvester = null;
                        IActorRef dispatcher = null;

                        actor.OnPreStart = context =>
                        {
                            Console.WriteLine("Connecting to Docker...");
                            context.System.Docker().RequestConnectLocal(context.Self);
                        };
                        
                        actor.Receive<Connected>((connected, context) =>
                        {
                            Console.WriteLine("Connected to Docker API (v{0}) at '{1}'.", connected.ApiVersion, connected.EndpointUri);
                            client = connected.Client;

                            Console.WriteLine("Initialising job store...");
                            jobStore = context.ActorOf( // TODO: Decide on supervision strategy.
                                JobStore.Create(Path.Combine(
                                    stateDirectory.FullName, "job-store.json"
                                ))
                            );
                            context.Watch(jobStore);

                            jobStore.Tell(
                                new EventBusActor.Subscribe(context.Self)
                            );
                        });
                        actor.Receive<EventBusActor.Subscribed>((subscribed, context) =>
                        {
                            Console.WriteLine("Job store initialised.");

                            Console.WriteLine("Initialising harvester...");
                            harvester = context.ActorOf(Props.Create(
                                () => new Harvester(stateDirectory, jobStore)
                            ));
                            Console.WriteLine("Harvester initialised.");

                            Console.WriteLine("Initialising dispatcher...");
                            launcher = context.ActorOf(
                                Props.Create(
                                    () => new Launcher(client)
                                ),
                                name: Launcher.ActorName
                            );
                            dispatcher = context.ActorOf(
                                Dispatcher.Create(stateDirectory, jobStore, launcher),
                                name: Dispatcher.ActorName
                            );
                            
                            // Wait for the dispatcher to start.
                            Thread.Sleep( // TODO: Find a better way.
                                TimeSpan.FromSeconds(1)
                            );

                            Console.WriteLine("Dispatcher initialised.");

                            Console.WriteLine("Creating job...");
                            jobStore.Tell(new JobStore.CreateJob(
                                targetUrl: new Uri("https://www.google.com/")
                            ));
                        });
                        actor.Receive<JobStoreEvents.JobCreated>((jobCreated, context) =>
                        {
                            Console.WriteLine("Job {0} created.", jobCreated.Job.Id);
                        });
                        actor.Receive<JobStoreEvents.JobStarted>((jobStarted, context) =>
                        {
                            Console.WriteLine("Job {0} started.", jobStarted.Job.Id);
                        });
                        actor.Receive<JobStoreEvents.JobCompleted>((jobStarted, context) =>
                        {
                            Console.WriteLine("Job {0} completed successfully.", jobStarted.Job.Id);
                            foreach (string jobMessage in jobStarted.Job.Messages)
                                Console.WriteLine("\t{0}", jobMessage);

                            Console.WriteLine("\tContent:");

                            foreach (string contentLine in (jobStarted.Job.Content ?? String.Empty).Split('\n'))
                                Console.WriteLine("\t{0}", contentLine);

                            completed.Set();
                        });
                        actor.Receive<JobStoreEvents.JobFailed>((jobStarted, context) =>
                        {
                            Console.WriteLine("Job {0} failed.", jobStarted.Job.Id);
                            foreach (string jobMessage in jobStarted.Job.Messages)
                                Console.WriteLine("\t{0}", jobMessage);

                            completed.Set();
                        });

                    }, name: "app");

                    completed.WaitOne();

                    // TODO: Find a better way to wait for container clean-up (e.g. create a wait-handle for Launcher that signals when all processes have exited).
                    Thread.Sleep(1000);

                    Console.WriteLine("Shutting down...");
                    system.Terminate().Wait();
                }

                Console.WriteLine("Shutdown complete.");
            }
            catch (Exception unexpectedError)
            {
                Console.WriteLine(unexpectedError);
            }
        }
    }
}
