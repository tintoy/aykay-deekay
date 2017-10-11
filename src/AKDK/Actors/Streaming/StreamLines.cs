using Akka.Actor;
using Akka.IO;
using System;
using System.IO;
using System.Text;

// Why the hell did they mark some of the most of the useful methods on ByteString as obsolete?
#pragma warning disable CS0618

namespace AKDK.Actors.Streaming
{
    using Messages;
    using Utilities;

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
        ///     The default name for instances of the <see cref="StreamLines"/> actor.
        /// </summary>
        public static readonly string ActorName = "stream-lines";

        /// <summary>
        ///		The bytes representing a Windows-style line terminator.
        /// </summary>
        static readonly string WindowsNewLine = "\r\n";

        /// <summary>
        ///		The bytes representing a Unix-style line terminator.
        /// </summary>
        static readonly string UnixNewLine = "\n";

        /// <summary>
        ///		<see cref="Props"/> used to create the <see cref="ReadStream"/> actor for reading from the underlying stream.
        /// </summary>
        Props               _readStreamProps;

        /// <summary>
        ///		The <see cref="ReadStream"/> actor used to read from the underlying stream.
        /// </summary>
        IActorRef           _readStream;

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
        /// <param name="encoding">
        ///     The expected stream encoding.
        /// </param>
        /// <param name="bufferSize">
        ///		The buffer size to use when reading from the stream.
        /// </param>
        /// <param name="windowsLineEndings">
        ///		Expect Windows-style line endings (CRLF) instead of Unix-style line endings (CR)?
        /// </param>
        /// 
        public StreamLines(string correlationId, IActorRef owner, Stream stream, Encoding encoding, int bufferSize, bool windowsLineEndings)
        {
            if (String.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(correlationId)}.", nameof(correlationId));

            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            _readStreamProps = ReadStream.Create(correlationId, Self, stream, bufferSize);

            ByteString lineEnding = ByteString.FromString(windowsLineEndings ? WindowsNewLine : UnixNewLine, encoding);
            ByteString buffer = ByteString.Empty;
            bool isEndOfStream = false;

            Receive<ReadStream.StreamData>(streamData =>
            {
                isEndOfStream = streamData.IsEndOfStream;
                buffer += streamData.Data;

                while (buffer.Count > 0)
                {
                    int lineEndingIndex = buffer.IndexOf(lineEnding);
                    if (lineEndingIndex == -1)
                    {
                        if (isEndOfStream)
                            lineEndingIndex = buffer.Count; // Publish whatever we have left.
                        else
                            break; // Wait for the rest of the current line to arrive.
                    }

                    var (left, right) = buffer.SplitAt(lineEndingIndex);
                    buffer = right.Drop(lineEnding.Count);

                    ByteString lineData = left;
                    string lineText = lineData.ToString(encoding);
                    System.Diagnostics.Debug.WriteLine($"StreamLines:PublishLine - '{lineText}'");

                    owner.Tell(
                        new StreamLine(streamData.CorrelationId,
                            line: lineText
                        )
                    );
                }

                if (isEndOfStream)
                {
                    owner.Tell(
                        new EndOfStream(correlationId)
                    );

                    Context.Stop(Self);
                }
            });
            Receive<ReadStream.StreamError>(error =>
            {
                owner.Tell(error);
            });
            Receive<ReadStream.Close>(close =>
            {
                _readStream.Forward(close);
            });
            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef == _readStream)
                {
                    if (!isEndOfStream)
                    {
                        owner.Tell(
                            new EndOfStream(correlationId)
                        );
                    }
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
        /// <param name="encoding">
        ///     The expected stream encoding.
        /// </param>
        /// <param name="bufferSize">
        ///		The buffer size to use when reading from the stream.
        /// </param>
        /// <param name="windowsLineEndings">
        ///		Expect Windows-style line endings (CRLF) instead of Unix-style line endings (CR)?
        /// </param>
        public static Props Create(string correlationId, IActorRef owner, Stream stream, Encoding encoding, int bufferSize = ReadStream.DefaultBufferSize, bool windowsLineEndings = false)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            return Props.Create(
                () => new StreamLines(correlationId, owner, stream, encoding, bufferSize, windowsLineEndings)
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