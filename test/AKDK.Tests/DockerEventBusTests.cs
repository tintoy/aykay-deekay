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
        public void DockerEventBus_Create_MonitorEvents_NoEvents()
        {
            IActorRef dockerEventBus = CreateDockerEventBus(subscribe: true);

            ConnectionTestProbe.ExpectMsg<Connection.ExecuteCommand>(executeCommand =>
            {
                ConnectionTestProbe.Reply(
                    new StreamedResponse(executeCommand.RequestMessage.CorrelationId,
                        responseStream: CreateEventStream( /* no events */ ),
                        format: StreamedResponseFormat.Events
                    )
                );
            });
            
            ExpectNoMsg();
        }

        IActorRef CreateDockerEventBus(string name = null, bool subscribe = false)
        {
            IActorRef client = CreateClient();

            IActorRef dockerEventBus = Sys.ActorOf(
                DockerEventBus.Create(client),
                name: name ?? DockerEventBus.ActorName
            );

            if (subscribe)
            {
                dockerEventBus.Tell(new EventBusActor.Subscribe(TestActor,
                    correlationId: nameof(DockerEventBus_Create_MonitorEvents_NoEvents)
                ));
                ExpectMsg<EventBusActor.Subscribed>(subscribed =>
                {
                    Assert.Equal(
                        expected: nameof(DockerEventBus_Create_MonitorEvents_NoEvents),
                        actual: subscribed.CorrelationId
                    );
                });
            }

            return dockerEventBus;
        }

        Stream CreateEventStream(params DockerEvent[] events)
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
