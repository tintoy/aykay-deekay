﻿using Akka.Actor;
using Akka.Actor.Dsl;
using Docker.DotNet.Models;
using System;
using System.Threading;

namespace AKDK.TestHarness
{
    using Actors;
    using Docker.DotNet;
    using Messages;
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

            try
            {
                IDockerClient dockerClient = new DockerClientConfiguration(LocalDocker.EndPointUri).CreateClient();
                using (ActorSystem system = ActorSystem.Create(name: "test-harness", config: AkkaConfig))
                {
                    IActorRef user = system.ActorOf(actor =>
                    {
                        // TODO: Fully implement ConnectionManager and use that rather than creating connection directly.

                        IActorRef client = null;

                        actor.Receive<ImageList>((imageList, context) =>
                        {
                            Console.WriteLine("Got {0} images:", imageList.Images.Count);
                            foreach (var image in imageList.Images)
                            {
                                Console.WriteLine("\t{0}", image.ID);

                                foreach (string repoTag in image.RepoTags)
                                    Console.WriteLine("\t\t{0}", repoTag);
                            }
                        });

                        actor.OnPreStart = context =>
                        {
                            Console.WriteLine("Connecting...");
                            client = context.ActorOf(
                                Props.Create<Client>(
                                    Connection.Create(dockerClient)
                                )                         
                            );

                            Console.WriteLine("Requesting image list...");
                            client.Tell(new ListImages(
                                new ImagesListParameters { All = true }
                            ));
                            client.Tell(
                                Connect.Local()
                            );
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