using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Configuration;
using AKDK.Actors;
using AKDK.Messages;
using System;
using System.IO;
using System.Threading;

namespace AKDK.Examples.Orchestration
{
    using Actors;
    using Messages;

    /// <summary>
    ///     Host for the orchestration example.
    /// </summary>
    static class Program
    {
        /// <summary>
        ///     Basic HOCON-style configuration for the test harness.
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

            ManualResetEvent completed = new ManualResetEvent(initialState: false);
            try
            {
                // Match colour for INFO messages from Akka logger.
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine("Starting ...");

                using (ActorSystem system = ActorSystem.Create("orchestration-example", AkkaConfig))
                {
                    Console.WriteLine("Running.");

                    system.ActorOf(actor =>
                    {
                        IActorRef jobStore = null;
                        IActorRef client = null;

                        actor.OnPreStart = context =>
                        {
                            Console.WriteLine("Initialising job store...");
                            jobStore = context.ActorOf( // TODO: Decide on supervision strategy.
                                JobStore.Create(Path.Combine(
                                    Directory.GetCurrentDirectory(), "job-store.json"
                                ))
                            );
                            context.Watch(jobStore);

                            jobStore.Tell(
                                new EventBusActor.Subscribe(context.Self)
                            );
                        };

                        actor.Receive<EventBusActor.Subscribed>((subscribed, context) =>
                        {
                            Console.WriteLine("Job store initialised.");

                            Console.WriteLine("Connecting to Docker...");
                            context.System.Docker().RequestConnectLocal(context.Self);
                        });
                        actor.Receive<Connected>((connected, context) =>
                        {
                            Console.WriteLine("Connected to Docker.");
                            client = connected.Client;

                            Console.WriteLine("Creating job...");
                            jobStore.Tell(new CreateJob(
                                targetUrl: new Uri("https://www.google.com/")
                            ));
                        });
                        actor.Receive<JobCreated>((jobCreated, context) =>
                        {
                            Console.WriteLine("Job {0} created.", jobCreated.JobId);

                            completed.Set();
                        });
                    });

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
