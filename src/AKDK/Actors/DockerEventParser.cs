using Akka.Actor;
using Akka.IO;
using System;
using System.IO;
using System.Text;

namespace AKDK.Actors
{
    using Messages.DockerEvents;
    using Streaming;

    /// <summary>
    ///		Actor that parses Docker event-stream data.
    /// </summary>
    public sealed class DockerEventParser
        : ReceiveActorEx
    {
        /// <summary>
        ///     The default name for instances of the <see cref="DockerEventParser"/> actor.
        /// </summary>
        public static readonly string ActorName = "event-parser";

        /// <summary>
        ///     The actor that owns the <see cref="DockerEventParser"/> actor (this actor will receive the stream data).
        /// </summary>
        readonly IActorRef _owner;

        /// <summary>
        ///		<see cref="Props"/> used to create the <see cref="ReadStream"/> actor for reading from the underlying stream.
        /// </summary>
        Props               _streamLinesProps;

        /// <summary>
        ///		The <see cref="StreamLines"/> actor used to read from the underlying stream.
        /// </summary>
        IActorRef           _streamLines;

        /// <summary>
        ///		Create a new <see cref="DockerEventParser"/> actor.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that will be sent with the stream data.
        /// </param>
        /// <param name="owner">
        ///		The actor that owns the <see cref="DockerEventParser"/> actor (this actor will receive the stream data).
        /// </param>
        /// <param name="stream">
        ///		The <see cref="Stream"/> to read from.
        /// </param>
        /// <param name="bufferSize">
        ///		The buffer size to use when reading from the stream.
        /// </param>
        public DockerEventParser(string correlationId, IActorRef owner, Stream stream, int bufferSize)
        {
            if (String.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(correlationId)}.", nameof(correlationId));

            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            _owner = owner;
            _streamLinesProps = StreamLines.Create(correlationId, Self, stream, Encoding.ASCII, bufferSize);

            Receive<StreamLines.StreamLine>(streamLine =>
            {
                var parsedEvent = DockerEvent.FromJson(streamLine.Line, correlationId);

                _owner.Tell(parsedEvent);
            });
            Receive<ReadStream.StreamError>(error =>
            {
                _owner.Tell(error);
            });
            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef.Equals(_owner))
                {
                    Log.Debug("Owner '{0}' terminated.", _owner);

                    Context.Stop(Self);
                }
                else if (terminated.ActorRef.Equals(_streamLines))
                {
                    Log.Debug("Streamer '{0}' terminated.", _streamLines);

                    Context.Stop(Self);
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

            _streamLines = Context.ActorOf(_streamLinesProps, StreamLines.ActorName);

            Context.Watch(_owner);
            Context.Watch(_streamLines);
        }

        /// <summary>
        ///		Generate <see cref="Props"/> to create a new <see cref="DockerEventParser"/> actor.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that will be sent with the stream data.
        /// </param>
        /// <param name="owner">
        ///		The actor that owns the <see cref="DockerEventParser"/> actor (this actor will receive the stream data).
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
                () => new DockerEventParser(correlationId, owner, stream, bufferSize)
            );
        }
    }
}