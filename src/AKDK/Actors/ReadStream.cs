using Akka.Actor;
using Akka.IO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AKDK.Actors
{
    public sealed class ReadStream
        : ReceiveActorEx
    {
        public static readonly StreamData EndOfStream = new StreamData(ByteString.Empty, isEndOfStream: true);

        readonly IActorRef  _owner;
        readonly Stream     _stream;
        readonly int        _bufferSize;
        readonly byte[]     _buffer;
        readonly bool       _closeStream;

        public ReadStream(IActorRef owner, Stream stream, int bufferSize = 1024, bool closeStream = true)
        {
            _owner = owner;
            _stream = stream;
            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
            _closeStream = closeStream;

            Receive<StreamData>(streamData =>
            {
                _owner.Tell(streamData);

                if (!streamData.IsEndOfStream)
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

            return new StreamData(
                ByteString.Create(_buffer, 0, bytesRead)
            );
        }

        public class StreamData
        {
            public StreamData(ByteString data, bool isEndOfStream = false)
            {
                Data = data;
            }

            public ByteString Data { get; }

            public bool IsEndOfStream { get; }
        }

        public class StreamError
        {
            public StreamError(Exception exception)
            {
                Exception = exception;
            }

            public Exception Exception { get; }
        }
    }
}