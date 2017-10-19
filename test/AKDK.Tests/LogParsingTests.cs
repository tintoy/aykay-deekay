using Akka.Actor;
using Akka.Configuration;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.IO;
using Akka.Streams.TestKit;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AKDK.Tests
{
    using Actors;
    using Messages;

    /// <summary>
    ///     Tests for <see cref="DockerLogParserStage"/>.
    /// </summary>
    public class LogParsingTests
        : TestKit
    {
        /// <summary>
        ///     The <see cref="Akka.TestKit.Xunit2.TestKit"/> for the current test.
        /// </summary>
        TestKit TestKit => this;

        /// <summary>
        ///     The <see cref="DockerLogParserStage"/> stage under test.
        /// </summary>
        DockerLogParserStage ParserStage { get; } = new DockerLogParserStage();

        /// <summary>
        ///     Verify that a <see cref="DockerLogParserStage"/> can parse Docker log entries with ASCII encoding.
        /// </summary>
        /// <param name="chunkSize">
        ///     The number of bytes to retrieve in each chunk from the source stream.
        /// </param>
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(50)]
        [Theory(DisplayName = "DockerLogParserStage can parse Docker log entries with ASCII encoding ")]
        public void Parse_ASCII_Success(int chunkSize)
        {
            Encoding encoding = Encoding.ASCII;

            TestSubscriber.Probe<DockerLogEntry> testProbe =
                StreamConverters.FromInputStream(
                    createInputStream: () => CreateLogStream(encoding),
                    chunkSize: chunkSize
                )
                .Via(new DockerLogParserStage())
                .RunWith(
                    TestKit.SinkProbe<DockerLogEntry>(),
                    Sys.Materializer()
                )
                .Request(3);

            DockerLogEntry logEntry = testProbe.ExpectNext();
            Assert.NotNull(logEntry);

            logEntry = testProbe.ExpectNext();
            Assert.NotNull(logEntry);

            logEntry = testProbe.ExpectNext();
            Assert.NotNull(logEntry);

            testProbe.ExpectComplete();
        }

        /// <summary>
        ///     Create a <see cref="MemoryStream"/> containing Docker log entries.
        /// </summary>
        /// <param name="encoding">
        ///     The encoding to use.
        /// </param>
        /// <returns>
        ///     The new <see cref="MemoryStream"/>.
        /// </returns>
        static MemoryStream CreateLogStream(Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            MemoryStream logStream = new MemoryStream();

            void WriteLogEntry(DockerLogStreamType streamType, string logEntry)
            {
                byte[] streamTypeData = BitConverter.GetBytes((int)DockerLogStreamType.StdOut);
                logStream.Write(streamTypeData, 0, streamTypeData.Length);

                byte[] frameLengthData = BitConverter.GetBytes(
                    encoding.GetByteCount(logEntry)
                );
                Array.Reverse(frameLengthData); // Big-endian.
                logStream.Write(frameLengthData, 0, frameLengthData.Length);

                byte[] frameData = encoding.GetBytes(logEntry);
                logStream.Write(frameData, 0, frameData.Length);
            }

            WriteLogEntry(DockerLogStreamType.StdOut, "ABC");
            WriteLogEntry(DockerLogStreamType.StdErr, "DEF");
            WriteLogEntry(DockerLogStreamType.StdOut, "GHI");
            
            logStream.Seek(0, SeekOrigin.Begin);

            return logStream;
        }
    }
}
