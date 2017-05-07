using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using System;
using System.IO;
using Xunit;

namespace AKDK.Tests
{
    using Actors.Streaming;

    public class ReadStreamTests
        : TestKit
    {
        static readonly Config TestConfig =
            ConfigurationFactory.ParseString(@"
                akka.suppress-json-serializer-warning = on
            ")
            .WithFallback(DefaultConfig);

        public ReadStreamTests()
            : base(TestConfig, actorSystemName: "read-stream-tests")
        {
        }

        [Fact]
        public void MemoryStream_1024_BufferSize_512()
        {
            const int streamLength = 1024;
            const int bufferSize = 512;

            TestProbe owner = CreateTestProbe(name: "owner");
            MemoryStream stream = CreateMemoryStream(streamLength);
            IActorRef readStream = ActorOf(
                ReadStream.Create("memory-stream-1024-buffer-size-512", owner, stream, bufferSize)
            );

            Within(TimeSpan.FromSeconds(1), () =>
            {
                // Exactly 2 packets
                for (int iteration = 0; iteration < streamLength / bufferSize; iteration++)
                {
                    owner.ExpectMsg<ReadStream.StreamData>(streamData =>
                    {
                        Assert.Equal(bufferSize, streamData.Data.Count);
                    });
                }
                owner.ExpectMsg<ReadStream.StreamData>(streamData =>
                {
                    Assert.Equal(0, streamData.Data.Count);
                    Assert.True(streamData.IsEndOfStream);
                });
            });
        }

        MemoryStream CreateMemoryStream(int length)
        {
            byte[] buffer = new byte[length];
            
            return new MemoryStream(buffer);
        }
    }
}
