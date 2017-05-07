using Akka.Actor;
using Akka.IO;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AKDK.Actors.Streaming
{
	/// <summary>
	///		Actor that reads lines from a stream.
	/// </summary>
	/// <remarks>
	///		This actor is only necessary until Akka.Streams for netstandard includes support for sourcing data from streams.
	/// </remarks>
	public sealed class StreamLines
		: ReceiveActorEx
	{
		static readonly ByteString WindowsNewLine = ByteString.FromString("\r\n", Encoding.Unicode);
		static readonly ByteString UnixNewLine = ByteString.FromString("\n", Encoding.Unicode);

		Props       _readStreamProps;
		IActorRef   _readStream;

		public StreamLines(string name, IActorRef owner, Stream stream, int bufferSize, bool windowsLineEndings)
		{
			_readStreamProps = ReadStream.Create(name, Self, stream, bufferSize);

			ByteString lineEnding = windowsLineEndings ? WindowsNewLine : UnixNewLine;
			ByteString buffer = ByteString.Empty;
			bool isEndOfStream = false;

			Receive<ReadStream.StreamData>(streamData =>
			{
				if (streamData.IsEndOfStream)
				{
					if (buffer.Count > 0)
					{
						owner.Tell(new StreamLine(streamData.Name,
							line: buffer.DecodeString(Encoding.Unicode)
						));
					}

					owner.Tell(
						new EndOfStream(name)
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
				owner.Tell(new StreamLine(streamData.Name, 
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

		protected override void PreStart()
		{
			base.PreStart();

			_readStream = Context.ActorOf(_readStreamProps, "read-stream");

			// Raise end-of-stream if source actor dies.
			Context.Watch(_readStream);
		}

		public static Props Create(string name, IActorRef owner, Stream stream, int bufferSize = ReadStream.DefaultBufferSize, bool windowsLineEndings = false)
		{
			return Props.Create(
				() => new StreamLines(name, owner, stream, bufferSize, windowsLineEndings)
			);
		}

		public class StreamLine
		{
			public StreamLine(string name, string line)
			{
				Name = name;
				Line = line;
			}

			public string Name { get; }
			public string Line { get; }
		}

		public class EndOfStream
		{
			public EndOfStream(string name)
			{
				Name = name;
			}
			public string Name { get; }
		}
	}
}