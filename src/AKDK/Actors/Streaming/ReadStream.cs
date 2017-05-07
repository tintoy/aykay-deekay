using Akka.Actor;
using Akka.IO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AKDK.Actors.Streaming
{
	/// <summary>
	///		Actor that reads data from a stream.
	/// </summary>
	/// <remarks>
	///		This actor is only necessary until Akka.Streams for netstandard includes support for sourcing data from streams.
	/// </remarks>
    public sealed class ReadStream
        : ReceiveActorEx
    {
        public const int DefaultBufferSize = 1024;

		readonly string		_name;
		readonly IActorRef  _owner;
        readonly Stream     _stream;
        readonly int        _bufferSize;
        readonly byte[]     _buffer;
        readonly bool       _closeStream;

        public ReadStream(string name, IActorRef owner, Stream stream, int bufferSize, bool closeStream)
        {
			_name = name;
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

		StreamData EndOfStream => new StreamData(_name, ByteString.Empty, isEndOfStream: true);


		protected override void PostStop()
        {
            if (_closeStream)
                _stream.Dispose();
        }

        async Task<StreamData> ReadData()
        {
            if (_stream.CanSeek && _stream.Position >= _stream.Length)
                return EndOfStream;

            int bytesRead = await _stream.ReadAsync(_buffer, 0, _buffer.Length);
            if (bytesRead == 0)
                return EndOfStream;

            return new StreamData(_name,
                ByteString.Create(_buffer, 0, bytesRead)
            );
        }

        public static Props Create(string name, IActorRef owner, Stream stream, int bufferSize = DefaultBufferSize, bool closeStream = true)
        {
            return Props.Create(
                () => new ReadStream(name, owner, stream, bufferSize, closeStream)
            );
        }

        public class StreamData
        {
            public StreamData(string name, ByteString data, bool isEndOfStream = false)
            {
				Name = name;
				Data = data;
                IsEndOfStream = isEndOfStream;
            }

			public string Name { get; }

            public ByteString Data { get; }

            public bool IsEndOfStream { get; }
        }

        public class StreamError
        {
            public StreamError(string name, Exception exception)
            {
				Name = name;
                Exception = exception;
            }

			public string Name { get; }

			public Exception Exception { get; }
        }
    }
}