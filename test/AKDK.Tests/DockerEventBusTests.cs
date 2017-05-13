using Akka.Actor;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace AKDK.Tests
{
    using Actors;
    using Messages;
    using Messages.DockerEvents;
    using Utilities;

    /// <summary>
    ///     Tests for the <see cref="DockerEventBus"/> actor.
    /// </summary>
    public class DockerEventBusTests
        : ConnectionTestKit
    {
        public DockerEventBusTests()
            : base(actorSystemName: "docker-event-bus-tests")
        {
        }

        [Fact]
        public void Create_NoEvents()
        {
            IActorRef dockerEventBus = CreateDockerEventBus(subscribe: true);
            StreamEvents(dockerEventBus);

            ExpectNoMsg();
        }

        [Fact]
        public void Event_NetworkDisconnected()
        {
            IActorRef dockerEventBus = CreateDockerEventBus(subscribe: true);

            StreamEvents(dockerEventBus,
                new NetworkDisconnected
                {
                    CorrelationId = DockerEventBus.ActorName,
                    Actor =
                    {
                        Id = "5846f2093cf2abd782e254d52e97bd6d4dc37331e20c18123db29cf2ca3d993b",
                        Attributes =
                        {
                            ["container"] = "13c3248c71242b49f30362a9a9a09d3308d4c99a423ada646cbd387fa996ac37",
                            ["name"] = "bridge",
                            ["type"] = "bridge"
                        }
                    },
                    TimestampUTC = DateTimeHelper.FromUnixSecondsUTC(1494477641)
                }
            );

            ExpectMsg<NetworkDisconnected>(networkDisconnected =>
            {
                Assert.Equal(networkDisconnected.CorrelationId, DockerEventBus.ActorName);
                Assert.Equal("5846f2093cf2abd782e254d52e97bd6d4dc37331e20c18123db29cf2ca3d993b", networkDisconnected.Actor.Id);
                Assert.Equal("13c3248c71242b49f30362a9a9a09d3308d4c99a423ada646cbd387fa996ac37", networkDisconnected.ContainerId);
            });
        }

        /// <summary>
        ///     Create a <see cref="DockerEventBus"/> actor using a client with a mocked-up connection.
        /// </summary>
        /// <param name="actorName">
        ///     An optional name for the actor.
        /// </param>
        /// <param name="subscribe">
        ///     Automatically subscribe to all event types?
        /// </param>
        /// <returns>
        ///     A reference to the newly-created actor.
        /// </returns>
        IActorRef CreateDockerEventBus(string actorName = null, bool subscribe = true)
        {
            if (actorName == null)
                actorName = DockerEventBus.ActorName;

            IActorRef client = CreateClient();

            IActorRef dockerEventBus = Sys.ActorOf(
                DockerEventBus.Create(client),
                name: actorName
            );

            if (subscribe)
            {
                dockerEventBus.Tell(new EventBusActor.Subscribe(TestActor,
                    correlationId: actorName
                ));
                ExpectMsg<EventBusActor.Subscribed>(subscribed =>
                {
                    Assert.Equal(
                        expected: actorName,
                        actual: subscribed.CorrelationId
                    );
                });
            }

            return dockerEventBus;
        }

        /// <summary>
        ///     Stream events to the specified actor.
        /// </summary>
        /// <param name="target">
        ///     The actor that will receive the events.
        /// </param>
        /// <param name="events">
        ///     The <see cref="DockerEvent"/>s to be streamed (existing correlation Ids, if any, will be preserved).
        /// </param>
        void StreamEvents(IActorRef target, params DockerEvent[] events)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (events == null)
                throw new ArgumentNullException(nameof(events));

            ConnectionTestProbe.ExpectMsg<Connection.ExecuteCommand>(executeCommand =>
            {
                Assert.IsType<MonitorContainerEvents>(executeCommand.RequestMessage);

                ConnectionTestProbe.ActorOf(
                    DockerEventParser.Create(
                        correlationId: target.Path.Name,
                        owner: target,
                        stream: CreateEventStream(events)
                    ),
                    name: DockerEventParser.ActorName
                );
            });
        }

        /// <summary>
        ///     Serialise one or more <see cref="DockerEvent"/>s to a stream.
        /// </summary>
        /// <param name="events">
        ///     The events.
        /// </param>
        /// <returns>
        ///     A <see cref="MemoryStream"/> containing the serialised event data.
        /// </returns>
        MemoryStream CreateEventStream(params DockerEvent[] events)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            MemoryStream eventStream = new MemoryStream();
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(eventStream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
                using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    foreach (DockerEvent dockerEvent in events)
                    {
                        dockerEvent.ToJson(jsonWriter);
                        jsonWriter.WriteWhitespace("\n");
                    }
                }
            }
            catch (Exception)
            {
                using (eventStream)
                {
                    throw;
                }
            }

            eventStream.Seek(0, SeekOrigin.Begin);

            return eventStream;
        }
    }
}
