using Akka.Actor;
using Docker.DotNet.Models;
using System.Linq;
using Xunit;

namespace AKDK.Tests
{
    using Actors;
    using Messages;

    /// <summary>
    ///     Tests for the <see cref="Client"/> actor.
    /// </summary>
    public class ClientTests
        : ConnectionTestKit
    {
        public ClientTests()
            : base(actorSystemName: "client-tests")
        {
        }

        [Fact]
        public void Client_Create_SendsNoMessageToConnection()
        {
            IActorRef client = CreateClient();
            ConnectionTestProbe.ExpectNoMsg();
        }

        [Fact]
        public void Client_ListImages_Success()
        {
            IActorRef client = CreateClient();
            client.Tell(
                new ListImages(new ImagesListParameters(),
                    correlationId: nameof(Client_ListImages_Success)
                )
            );
            ConnectionTestProbe.ExpectMsg<Connection.ExecuteCommand>(executeCommand =>
            {
                Assert.IsType<ListImages>(executeCommand.RequestMessage);
                Assert.Equal(
                    expected: nameof(Client_ListImages_Success),
                    actual: executeCommand.CorrelationId
                );
                Assert.Equal(
                    expected: nameof(Client_ListImages_Success),
                    actual: executeCommand.RequestMessage.CorrelationId
                );

                ConnectionTestProbe.Reply(new ImageList(executeCommand.CorrelationId,
                    images: Enumerable.Empty<ImagesListResponse>()
                ));
            });

            ExpectMsg<ImageList>(imageList =>
            {
                Assert.Equal(
                    expected: nameof(Client_ListImages_Success),
                    actual: imageList.CorrelationId
                );
                Assert.Equal(0, imageList.Images.Count);
            });
        }
    }
}
