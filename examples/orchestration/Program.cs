using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Event;
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
        ///     Basic HOCON-style configuration for the orchestration example.
        /// </summary>
        static readonly Config AkkaConfig = ConfigurationFactory.ParseString(
            @"
                akka {
                    loglevel = INFO
                    stdout-loglevel = INFO
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
                        ILoggingAdapter log = null;
                        IActorRef client = null;
                        IActorRef jobStore = null;
                        IActorRef launcher = null;
                        IActorRef harvester = null;
                        IActorRef dispatcher = null;

                        actor.OnPreStart = context =>
                        {
                            log = context.GetLogger();

                            log.Info("Connecting to Docker...");
                            context.System.Docker().RequestConnectLocal(context.Self);
                        };
                        
                        actor.Receive<Connected>((connected, context) =>
                        {
                            log.Info("Connected to Docker API (v{0}) at '{1}'.", connected.ApiVersion, connected.EndpointUri);
                            client = connected.Client;

                            log.Info("Initialising job store...");
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
                            log.Info("Job store initialised.");

                            log.Info("Initialising harvester...");
                            harvester = context.ActorOf(Props.Create(
                                () => new Harvester(stateDirectory, jobStore)
                            ));
                            log.Info("Harvester initialised.");

                            log.Info("Initialising dispatcher...");
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

                            log.Info("Dispatcher initialised.");

                            log.Info("Creating job...");
                            jobStore.Tell(new JobStore.CreateJob(
                                targetUrl: new Uri("https://ifconfig.co/json")
                            ));
                        });
                        actor.Receive<JobStoreEvents.JobCreated>((jobCreated, context) =>
                        {
                            log.Info("Job {0} created.", jobCreated.Job.Id);
                        });
                        actor.Receive<JobStoreEvents.JobStarted>((jobStarted, context) =>
                        {
                            log.Info("Job {0} started.", jobStarted.Job.Id);

                            dispatcher.Tell(
                                new Dispatcher.NotifyWhenAllJobsCompleted()
                            );
                        });
                        actor.Receive<JobStoreEvents.JobCompleted>((jobStarted, context) =>
                        {
                            log.Info("Job {0} completed successfully.", jobStarted.Job.Id);
                            foreach (string jobMessage in jobStarted.Job.Messages)
                                log.Info("-- {0}", jobMessage);

                            log.Info("-- Content:");

                            foreach (string contentLine in (jobStarted.Job.Content ?? String.Empty).Split('\n'))
                                log.Info("---- {0}", contentLine);
                        });
                        actor.Receive<JobStoreEvents.JobFailed>((jobStarted, context) =>
                        {
                            log.Info("Job {0} failed.", jobStarted.Job.Id);
                            foreach (string jobMessage in jobStarted.Job.Messages)
                                log.Info("-- {0}", jobMessage);
                        });
                        actor.Receive<Dispatcher.AllJobsCompleted>((allJobsCompleted, context) =>
                        {
                            log.Info("All jobs completed.");

                            completed.Set();
                        });

                    }, name: "app");

                    completed.WaitOne();
                    
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
