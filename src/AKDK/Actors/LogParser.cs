using Akka.Actor;
using Akka.IO;
using System;
using System.IO;

namespace AKDK.Actors
{
    using Messages;
    using Streaming;

    /// <summary>
    ///		Actor that parses Docker log entries stream data.
    /// </summary>
    public sealed class LogParser
        : ReceiveActorEx
    {
        /// <summary>
        ///		<see cref="Props"/> used to create the <see cref="ReadStream"/> actor for reading from the underlying stream.
        /// </summary>
        Props _readStreamProps;

        /// <summary>
        ///		The <see cref="ReadStream"/> actor used to read from the underlying stream.
        /// </summary>
        IActorRef _readStream;

        /// <summary>
        ///		Create a new <see cref="LogParser"/> actor.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that will be sent with the stream data.
        /// </param>
        /// <param name="owner">
        ///		The actor that owns the <see cref="LogParser"/> actor (this actor will receive the stream data).
        /// </param>
        /// <param name="stream">
        ///		The <see cref="Stream"/> to read from.
        /// </param>
        /// <param name="bufferSize">
        ///		The buffer size to use when reading from the stream.
        /// </param>
        public LogParser(string correlationId, IActorRef owner, Stream stream, int bufferSize)
        {
            if (String.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(correlationId)}.", nameof(correlationId));

            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            _readStreamProps = ReadStream.Create(correlationId, Self, stream, bufferSize);

            ByteString buffer = ByteString.Empty;
            bool isEndOfStream = false;

            // Parse as many log entries as we can find in the buffer.
            void LogPump()
            {
                DockerLogEntry logEntry;
                while (buffer.Count > 0)
                {
                    (logEntry, buffer) = DockerLogEntry.ReadFrom(buffer, correlationId);
                    if (logEntry == null)
                        break;

                    owner.Tell(logEntry);
                }
            }

            // We've reached the end of the log; like tears in rain, time to die.
            void EndOfStream()
            {
                if (isEndOfStream)
                    return;

                isEndOfStream = true;

                owner.Tell(
                    new EndOfLog(correlationId)
                );
                Context.Stop(Self);
            }

            Receive<ReadStream.StreamData>(streamData =>
            {
                buffer += streamData.Data;
                LogPump();

                if (streamData.IsEndOfStream)
                    EndOfStream();
            });
            Receive<ReadStream.StreamError>(error =>
            {
                owner.Tell(error);
            });
            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef == _readStream)
                    EndOfStream();
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
        ///		Generate <see cref="Props"/> to create a new <see cref="LogParser"/> actor.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that will be sent with the stream data.
        /// </param>
        /// <param name="owner">
        ///		The actor that owns the <see cref="LogParser"/> actor (this actor will receive the stream data).
        /// </param>
        /// <param name="stream">
        ///		The <see cref="Stream"/> to read from.
        /// </param>
        /// <param name="bufferSize">
        ///		The buffer size to use when reading from the stream.
        /// </param>
        public static Props Create(string correlationId, IActorRef owner, Stream stream, int bufferSize = ReadStream.DefaultBufferSize)
        {
            return Props.Create(
                () => new LogParser(correlationId, owner, stream, bufferSize)
            );
        }
    }
}
