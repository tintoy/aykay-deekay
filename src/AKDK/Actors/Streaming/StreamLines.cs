using Akka;
using Akka.Actor;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.IO;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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
        ///     The source used to stream data from the underlying <see cref="Stream"/>.
        /// </summary>
        readonly Source<ByteString, Task<IOResult>>     _streamSource;

        /// <summary>
        ///     The <see cref="Flow{TIn, TOut, TMat}"/> used to process each line of data.
        /// </summary>
        readonly Flow<ByteString, StreamLine, NotUsed>  _lineProcessor;

        /// <summary>
        ///     A sink used to send <see cref="StreamLine"/>s to the owning actor.
        /// </summary>
        readonly Sink<StreamLine, NotUsed>              _ownerSink;

        /// <summary>
        ///     The processing graph used to stream lines to the owner actor.
        /// </summary>
        readonly IRunnableGraph<Task<IOResult>>         _lineStreamer;

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

            // AF: This (experimental) code works, but is incomplete. Doesn't handle errors, cancellation, or cleaning up the actor once the graph's work is complete.

            _streamSource = StreamConverters.FromInputStream(
                createInputStream: () => stream,
                chunkSize: 3
            );

            _lineProcessor =
                Framing.Delimiter(
                    delimiter: ByteString.FromString(windowsLineEndings ? WindowsNewLine : UnixNewLine, encoding),
                    maximumFrameLength: 1024 * 1024,
                    allowTruncation: true
                )
                .Select(line =>
                    new StreamLine(correlationId, line.Compact().ToString(encoding))
                );

            _ownerSink = Sink.ActorRef<StreamLine>(owner,
                onCompleteMessage: new EndOfStream(correlationId)
            );

            _lineStreamer = _streamSource.Via(_lineProcessor).To(_ownerSink);

            Receive<IOResult>(result =>
            {
                if (!result.WasSuccessful)
                    Log.Error(result.Error, "Error while reading from stream.");
            });
        }

        /// <summary>
        ///		Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            _lineStreamer.Run(Context.System.Materializer()).PipeTo(Self);
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
                encoding = Encoding.Unicode;

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