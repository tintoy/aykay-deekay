using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace AKDK.Tests
{
    using Actors.Streaming;

    public class StreamLinesTests
        : TestKit
    {
        static readonly Config TestConfig =
            ConfigurationFactory.ParseString(@"
                akka.suppress-json-serializer-warning = on
            ")
            .WithFallback(DefaultConfig);

        public StreamLinesTests()
            : base(TestConfig, actorSystemName: "read-stream-tests")
        {
        }

        [Fact]
        public void MemoryStream_Lines_2_BufferSize_3()
        {
            const int bufferSize = 3;
            Encoding encoding = Encoding.UTF8;

            TestProbe owner = CreateTestProbe(name: "owner");
            MemoryStream stream = CreateMemoryStream("ABCDE\nFGHIJ", encoding);
            IActorRef streamLines = ActorOf(
                StreamLines.Create("lines-2-buffer-size-3", owner, stream, encoding, bufferSize)
            );

            Within(TimeSpan.FromSeconds(5), () =>
            {
                owner.ExpectMsg<StreamLines.StreamLine>(streamLine =>
                {
                    Assert.Equal("ABCDE", streamLine.Line);
                });
                owner.ExpectMsg<StreamLines.StreamLine>(streamLine =>
                {
                    Assert.Equal("FGHIJ", streamLine.Line);
                });
                owner.ExpectMsg<StreamLines.EndOfStream>();
                owner.ExpectNoMsg();
            });
        }

        [Fact]
        public void MemoryStream_Lines_2_Trailing_NewLine_BufferSize_3()
        {
            const int bufferSize = 3;
            Encoding encoding = Encoding.UTF8;

            TestProbe owner = CreateTestProbe(name: "owner");
            MemoryStream stream = CreateMemoryStream("ABCDE\nFGHIJ\n", encoding);
            IActorRef streamLines = ActorOf(
                StreamLines.Create("lines-2-buffer-size-3", owner, stream, encoding, bufferSize)
            );

            Within(TimeSpan.FromSeconds(5), () =>
            {
                owner.ExpectMsg<StreamLines.StreamLine>(streamLine =>
                {
                    Assert.Equal("ABCDE", streamLine.Line);
                });
                owner.ExpectMsg<StreamLines.StreamLine>(streamLine =>
                {
                    Assert.Equal("FGHIJ", streamLine.Line);
                });
                owner.ExpectMsg<StreamLines.EndOfStream>();
                owner.ExpectNoMsg();
            });
        }

        [Fact]
        public void MemoryStream_JustNewLines_3_BufferSize_2()
        {
            const int bufferSize = 2;
            Encoding encoding = Encoding.UTF8;

            TestProbe owner = CreateTestProbe(name: "owner");
            MemoryStream stream = CreateMemoryStream("\n\n\n", encoding);
            IActorRef streamLines = ActorOf(
                StreamLines.Create("just-new-lines-3-buffer-size-2", owner, stream, encoding, bufferSize),
                name: "stream-lines"
            );

            Within(TimeSpan.FromSeconds(5), () =>
            {
                owner.ExpectMsg<StreamLines.StreamLine>(streamLine =>
                {
                    Assert.Equal(String.Empty, streamLine.Line);
                });
                owner.ExpectMsg<StreamLines.StreamLine>(streamLine =>
                {
                    Assert.Equal(String.Empty, streamLine.Line);
                });
                owner.ExpectMsg<StreamLines.StreamLine>(streamLine =>
                {
                    Assert.Equal(String.Empty, streamLine.Line);
                });
                owner.ExpectMsg<StreamLines.EndOfStream>();
                owner.ExpectNoMsg();
            });
        }

        MemoryStream CreateMemoryStream(string content, Encoding encoding)
        {
            return new MemoryStream(
                encoding.GetBytes(content)
            );
        }
    }
}
