using Akka.Actor;
using Akka.IO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AKDK.Actors.Streaming
{
	using Messages;

	/// <summary>
	///		Actor that reads data from a stream.
	/// </summary>
	/// <remarks>
	///		This actor is only necessary until Akka.Streams for netstandard includes support for sourcing data from streams.
	/// </remarks>
    public sealed class ReadStream
        : ReceiveActorEx
    {
		/// <summary>
		///		The default buffer size used by the <see cref="ReadStream"/> actor.
		/// </summary>
        public const int DefaultBufferSize = 1024;

		/// <summary>
		///		The correlation Id to be sent with messages from the <see cref="ReadStream"/> actor.
		/// </summary>
		readonly string		_correlationId;

		/// <summary>
		///		The actor that owns the stream (this actor will receive the stream data).
		/// </summary>
		readonly IActorRef  _owner;

		/// <summary>
		///		The <see cref="Stream"/> to read from.
		/// </summary>
		readonly Stream     _stream;

		/// <summary>
		///		The buffer size to use when reading from the underlying <see cref="Stream"/>.
		/// </summary>
		readonly int        _bufferSize;

		/// <summary>
		///		The buffer used when reading from the underlying <see cref="Stream"/>.
		/// </summary>
		readonly byte[]     _buffer;

		/// <summary>
		///		Close the underlying <see cref="Stream"/> when end-of-stream is reached?
		/// </summary>
		readonly bool       _closeStream;

		/// <summary>
		///		Create a new <see cref="ReadStream"/> actor.
		/// </summary>
		/// <param name="correlationId">
		///		The correlation Id to be sent with the stream data.
		/// </param>
		/// <param name="owner">
		///		The actor that owns the stream (this actor will receive the stream data).
		/// </param>
		/// <param name="stream">
		///		The <see cref="Stream"/> to read from.
		/// </param>
		/// <param name="bufferSize">
		///		The buffer size to use when reading from the underlying <paramref name="stream"/>.
		/// </param>
		/// <param name="closeStream">
		///		Close the underlying <paramref name="stream"/> when end-of-stream is reached?
		/// </param>
		public ReadStream(string correlationId, IActorRef owner, Stream stream, int bufferSize, bool closeStream)
        {
			if (string.IsNullOrWhiteSpace(correlationId))
				throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(correlationId)}.", nameof(correlationId));

			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			_correlationId = correlationId;
			_owner = owner;
            _stream = stream;
            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
            _closeStream = closeStream;

            Receive<StreamData>(streamData =>
            {
                _owner.Tell(streamData);

                if (streamData.IsEndOfStream)
					Context.Stop(Self);
				else
                    ReadData().PipeTo(Self);
            });
            Receive<Failure>(readFailure =>
            {
                Log.Error(readFailure.Exception, "Unexpected error while reading from stream: {0}",
                    readFailure.Exception.Message
                );

                _owner.Tell(readFailure);
            });

            // Kick off initial read.
            ReadData().PipeTo(Self);
        }

		/// <summary>
		///		<see cref="StreamData"/> representing the end of the stream.
		/// </summary>
		StreamData EndOfStream => new StreamData(_correlationId, ByteString.Empty, isEndOfStream: true);

		/// <summary>
		///		Called when the actor is stoppped.
		/// </summary>
		protected override void PostStop()
        {
            if (_closeStream)
                _stream.Dispose();
        }

		/// <summary>
		///		Asynchronously read data from the stream.
		/// </summary>
		/// <returns>
		///		A <see cref="StreamData"/> containing the stream data (as a <see cref="ByteString"/>).
		/// </returns>
        async Task<StreamData> ReadData()
        {
            if (_stream.CanSeek && _stream.Position >= _stream.Length)
                return EndOfStream;

            int bytesRead = await _stream.ReadAsync(_buffer, 0, _buffer.Length);
            if (bytesRead == 0)
                return EndOfStream;

            return new StreamData(_correlationId,
                ByteString.Create(_buffer, 0, bytesRead)
            );
        }

		/// <summary>
		///		Generate <see cref="Props"/> to create a new <see cref="ReadStream"/> actor.
		/// </summary>
		/// <param name="correlationId">
		///		The correlation Id to be sent with the stream data.
		/// </param>
		/// <param name="owner">
		///		The actor that owns the stream (this actor will receive the stream data).
		/// </param>
		/// <param name="stream">
		///		The <see cref="Stream"/> to read from.
		/// </param>
		/// <param name="bufferSize">
		///		The buffer size to use when reading from the <paramref name="stream"/>.
		/// </param>
		/// <param name="closeStream">
		///		Close the <paramref name="stream"/> when end-of-stream is reached?
		/// </param>
		public static Props Create(string correlationId, IActorRef owner, Stream stream, int bufferSize = DefaultBufferSize, bool closeStream = true)
        {
            return Props.Create(
                () => new ReadStream(correlationId, owner, stream, bufferSize, closeStream)
            );
        }

		/// <summary>
		///		Represents data from a <see cref="Stream"/>.
		/// </summary>
        public class StreamData
			: CorrelatedMessage
        {
			/// <summary>
			///		Create a new <see cref="StreamData"/> message.
			/// </summary>
			/// <param name="correlationId">
			///		The message correlation Id.
			/// </param>
			/// <param name="data">
			///		The stream data, as a <see cref="ByteString"/>.
			/// </param>
			/// <param name="isEndOfStream">
			///		Has the end of the stream been reached?
			/// </param>
            public StreamData(string correlationId, ByteString data, bool isEndOfStream = false)
				: base(correlationId)
            {
				Data = data;
                IsEndOfStream = isEndOfStream;
            }

			/// <summary>
			///		The stream data, as a <see cref="ByteString"/>.
			/// </summary>
			public ByteString Data { get; }

			/// <summary>
			///		Has the end of the stream been reached?
			/// </summary>
			public bool IsEndOfStream { get; }
        }

		/// <summary>
		///		Represents an error encountered when reading from a stream.
		/// </summary>
        public class StreamError
			: CorrelatedMessage
        {
			/// <summary>
			///		Create a new <see cref="StreamError"/> message.
			/// </summary>
			/// <param name="correlationId">
			///		The message correlation Id.
			/// </param>
			/// <param name="exception">
			///		An <see cref="System.Exception"/> representing error.
			/// </param>
            public StreamError(string correlationId, Exception exception)
				: base(correlationId)
            {
                Exception = exception;
            }

			/// <summary>
			///		An <see cref="System.Exception"/> representing error.
			/// </summary>
			public Exception Exception { get; }
        }
    }
}