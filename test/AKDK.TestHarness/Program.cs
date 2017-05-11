using Akka.Actor;
using Akka.Actor.Dsl;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AKDK.TestHarness
{
    using Actors.Streaming;
    using Messages;

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

            ManualResetEvent completed = new ManualResetEvent(initialState: false);
            try
            {
                // Match colour for INFO messages from Akka logger.
                Console.ForegroundColor = ConsoleColor.White;

                using (ActorSystem system = ActorSystem.Create(name: "test-harness", config: AkkaConfig))
                {
                    IActorRef user = system.ActorOf(actor =>
                    {
                        IActorRef client = null;

                        actor.OnPreStart = context =>
                        {
                            Console.WriteLine("Connecting...");
                            context.System.Docker().RequestConnectLocal(context.Self);
                        };

                        actor.Receive<Connected>((connected, context) =>
                        {
                            Console.WriteLine("Connected.");
                            client = connected.Client;

                            Console.WriteLine("Requesting image list...");
                            client.Tell(new ListImages(
                                new ImagesListParameters { All = true }
                            ));
                        });
                        actor.Receive<ConnectFailed>((connectFailed, context) =>
                        {
                            Console.WriteLine("Failed to connect. {0}",
                                connectFailed.Exception.Message
                            );

                            Thread.Sleep(
                                TimeSpan.FromSeconds(5)
                            );
                            Console.WriteLine("Reconnecting...");
                            context.System.Docker().RequestConnectLocal(context.Self);
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

                            const string imageName = "hello-world:latest";

                            Console.WriteLine("Requesting creation of container from image '{0}'...", imageName);
                            CreateContainerParameters createContainerParameters = new CreateContainerParameters
                            {
                                Image = imageName,
                                AttachStdout = true,
                                AttachStderr = true,
                                Tty = false
                            };

                            // Needed if you want to get logs from the API ("systemd" logger is also supported).
                            createContainerParameters.UseJsonFileLogger();

                            client.Tell(
                                new CreateContainer(createContainerParameters)
                            );
                        });
                        actor.Receive<ContainerCreated>((containerCreated, context) =>
                        {
                            // AF: You could use CorrelationId here to match up CreateContainer with ContainerCreated.

                            Console.WriteLine("Container '{0}' created.", containerCreated.ContainerId);
                            if (containerCreated.Warnings.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                foreach (string warning in containerCreated.ApiResponse.Warnings)
                                    Console.WriteLine("\t{0}", warning);

                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            Console.WriteLine("Starting container '{0}'...", containerCreated.ContainerId);
                            client.Tell(
                                new StartContainer(containerCreated.ContainerId)
                            );
                        });
                        actor.Receive<ContainerStarted>((containerStarted, context) =>
                        {
                            Console.WriteLine("Started container '{0}'.", containerStarted.ContainerId);
                            Console.WriteLine("Waiting 5 seconds for container process to exit...");

                            Thread.Sleep(
                                TimeSpan.FromSeconds(5)
                            );

                            string containerId = containerStarted.ContainerId;

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

                            completed.Set();
                        });

                    }, "docker-user");
                    
                    Console.WriteLine("Running.");
                    completed.WaitOne();

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
