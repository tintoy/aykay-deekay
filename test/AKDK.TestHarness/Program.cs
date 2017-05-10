using Akka.Actor;
using Akka.Actor.Dsl;
using Docker.DotNet.Models;
using System;
using System.Threading;

namespace AKDK.TestHarness
{
    using Actors.Streaming;
    using Docker.DotNet;
    using Messages;
    using System.Text.RegularExpressions;
    using Utilities;

    /// <summary>
    ///     Test harness for AKDK.
    /// </summary>
    static class Program
    {
        /// <summary>
        ///     Basic HOCON-style configuration for the test harness.
        /// </summary>
        static readonly Akka.Configuration.Config AkkaConfig = Akka.Configuration.ConfigurationFactory.ParseString(
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

            try
            {
                // Match colour for INFO messages from Akka logger.
                Console.ForegroundColor = ConsoleColor.White;

                using (ActorSystem system = ActorSystem.Create(name: "test-harness", config: AkkaConfig))
                {
                    IActorRef user = system.ActorOf(actor =>
                    {
                        // TODO: Fully implement ConnectionManager and use that rather than creating connection directly.

                        IActorRef client = null;

                        actor.Receive<Connected>((connected, context) =>
                        {
                            Console.WriteLine("Connected.");
                            client = connected.Client;

                            Console.WriteLine("Requesting image list...");
                            client.Tell(new ListImages(
                                new ImagesListParameters { All = true }
                            ));
                        });
                        actor.Receive<ImageList>((imageList, context) =>
                        {
                            Console.WriteLine("Got {0} images:", imageList.Images.Count);
                            foreach (var image in imageList.Images)
                            {
                                Console.WriteLine("\t{0}", image.ID);

                                foreach (string repoTag in image.RepoTags)
                                    Console.WriteLine("\t\t{0}", repoTag);
                            }

                            const string containerId = "b2ebc0e522e3";

                            Console.WriteLine("Asking for logs of container '{0}'...", containerId);
                            client.Tell(new GetContainerLogs(containerId, new ContainerLogsParameters
                            {
                                ShowStderr = true,
                                ShowStdout = true
                            }));
                        });
                        actor.Receive<StreamLines.StreamLine>((streamLine, context) =>
                        {
                            Console.WriteLine("{0}: Got log line: '{1}'", streamLine.CorrelationId, streamLine.Line);
                        });
                        actor.Receive<StreamLines.EndOfStream>((endOfStream, context) =>
                        {
                            Console.WriteLine("{0}: Got end-of-stream.", endOfStream.CorrelationId);
                        });

                        actor.OnPreStart = context =>
                        {
                            Console.WriteLine("Connecting...");
                            context.System.Docker().RequestConnectLocal(context.Self);
                        };

                    }, "docker-user");
                    
                    Console.WriteLine("Running (press enter to terminate).");
                    Console.ReadLine();

                    system.Terminate().Wait();
                }
            }
            catch (Exception unexpectedError)
            {
                Console.WriteLine(unexpectedError);
            }
        }
    }
}
