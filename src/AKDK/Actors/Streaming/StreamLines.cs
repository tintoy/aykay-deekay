using Akka.Actor;
using Akka.IO;
using System.IO;
using System.Text;

namespace AKDK.Actors.Streaming
{
    using Messages;

    /// <summary>
    ///		Actor that reads lines from a stream.
    /// </summary>
    /// <remarks>
    ///		This actor is only necessary until Akka.Streams for netstandard includes support for sourcing data from streams.
    /// </remarks>
    public sealed class StreamLines
        : ReceiveActorEx
    {
        /// <summary>
        ///		The bytes representing a Windows-style line terminator.
        /// </summary>
        static readonly ByteString WindowsNewLine = ByteString.FromString("\r\n", Encoding.Unicode);

        /// <summary>
        ///		The bytes representing a Unix-style line terminator.
        /// </summary>
        static readonly ByteString UnixNewLine = ByteString.FromString("\n", Encoding.Unicode);

        /// <summary>
        ///		<see cref="Props"/> used to create the <see cref="ReadStream"/> actor for reading from the underlying stream.
        /// </summary>
        Props       _readStreamProps;

        /// <summary>
        ///		The <see cref="ReadStream"/> actor used to read from the underlying stream.
        /// </summary>
        IActorRef   _readStream;

        /// <summary>
        ///		Create a new <see cref="StreamLines"/> actor.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that will be sent with the stream data.
        /// </param>
        /// <param name="owner">
        ///		The actor that owns the <see cref="StreamLines"/> actor (this actor will receive the stream data).
        /// </param>
        /// <param name="stream">
        ///		The <see cref="Stream"/> to read from.
        /// </param>
        /// <param name="bufferSize">
        ///		The buffer size to use when reading from the stream.
        /// </param>
        /// <param name="windowsLineEndings">
        ///		Expect Windows-style line endings (CRLF) instead of Unix-style line endings (CR)?
        /// </param>
        public StreamLines(string correlationId, IActorRef owner, Stream stream, int bufferSize, bool windowsLineEndings)
        {
            _readStreamProps = ReadStream.Create(correlationId, Self, stream, bufferSize);

            ByteString lineEnding = windowsLineEndings ? WindowsNewLine : UnixNewLine;
            ByteString buffer = ByteString.Empty;
            bool isEndOfStream = false;

            Receive<ReadStream.StreamData>(streamData =>
            {
                if (streamData.IsEndOfStream)
                {
                    if (buffer.Count > 0)
                    {
                        owner.Tell(new StreamLine(streamData.CorrelationId,
                            line: buffer.DecodeString(Encoding.Unicode)
                        ));
                    }

                    owner.Tell(
                        new EndOfStream(correlationId)
                    );
                    Context.Stop(Self);

                    return;
                }

                buffer += streamData.Data;

                int lineEndingIndex = buffer.IndexOf(lineEnding);
                if (lineEndingIndex == -1)
                    return;

                var split = buffer.SplitAt(lineEndingIndex);
                buffer = split.Item2.Drop(lineEnding.Count);

                ByteString line = split.Item1;
                owner.Tell(new StreamLine(streamData.CorrelationId,
                    line: line.DecodeString(Encoding.Unicode)
                ));
            });
            Receive<ReadStream.StreamError>(error =>
            {
                owner.Tell(error);
            });
            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef == owner)
                {
                    if (isEndOfStream)
                        return;
                }
                else
                    Unhandled(terminated);
            });
        }

        /// <summary>
        ///		Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            _readStream = Context.ActorOf(_readStreamProps, "read-stream");

            // Raise end-of-stream if source actor dies.
            Context.Watch(_readStream);
        }

        /// <summary>
        ///		Generate <see cref="Props"/> to create a new <see cref="StreamLines"/> actor.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that will be sent with the stream data.
        /// </param>
        /// <param name="owner">
        ///		The actor that owns the <see cref="StreamLines"/> actor (this actor will receive the stream data).
        /// </param>
        /// <param name="stream">
        ///		The <see cref="Stream"/> to read from.
        /// </param>
        /// <param name="bufferSize">
        ///		The buffer size to use when reading from the stream.
        /// </param>
        /// <param name="windowsLineEndings">
        ///		Expect Windows-style line endings (CRLF) instead of Unix-style line endings (CR)?
        /// </param>
        public static Props Create(string correlationId, IActorRef owner, Stream stream, int bufferSize = ReadStream.DefaultBufferSize, bool windowsLineEndings = false)
        {
            return Props.Create(
                () => new StreamLines(correlationId, owner, stream, bufferSize, windowsLineEndings)
            );
        }

        /// <summary>
        ///		Represents a line of text from a stream (without the line terminator).
        /// </summary>
        public class StreamLine
            : CorrelatedMessage
        {
            /// <summary>
            ///		Create a new <see cref="StreamLine"/> message.
            /// </summary>
            /// <param name="correlationId">
            ///		The message correlation Id.
            /// </param>
            /// <param name="line">
            ///		The line of text.
            /// </param>
            public StreamLine(string correlationId, string line)
                : base(correlationId)
            {
                Line = line;
            }

            /// <summary>
            ///		The line of text.
            /// </summary>
            public string Line { get; }
        }

        /// <summary>
        ///		Represents the end of a stream.
        /// </summary>
        public class EndOfStream
        {
            /// <summary>
            ///		Create a new <see cref="EndOfStream"/> message.
            /// </summary>
            /// <param name="correlationId">
            ///		The message correlation Id.
            /// </param>
            public EndOfStream(string correlationId)
            {
                CorrelationId = correlationId;
            }

            /// <summary>
            ///		The message correlation Id.
            /// </summary>
            public string CorrelationId { get; }
        }
    }
}