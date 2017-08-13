using Akka.IO;
using System;
using System.Text;

// Why the hell did they mark some of the most of the useful methods on ByteString as obsolete?
#pragma warning disable CS0618

namespace AKDK.Messages
{
    using Utilities;

	/// <summary>
	///     The header for a line from a Docker container log.
	/// </summary>
	public class DockerLogEntry
        : CorrelatedMessage
	{
        /// <summary>
        ///     The default encoding used in Docker logs.
        /// </summary>
        public static readonly Encoding DefaultEncoding = Encoding.ASCII;

        /// <summary>
        ///     The length of the header for a Docker log entry.
        /// </summary>
        public const int HeaderLength = 8;

        /// <summary>
		///     The 0-based offset of the frame size bytes within the header for a Docker log entry.
		/// </summary>
		public const int HeaderFrameSizeOffset = 4;

		/// <summary>
		///     Create a new <see cref="DockerLogEntry"/>.
		/// </summary>
		/// <param name="streamType">
		///     The type of stream (e.g. STDOUT, STDERR) represented by the <see cref="DockerLogEntry"/>.
		/// </param>
		/// <param name="data">
		///     The log entry data.
		/// </param>
        /// <param name="correlationId">
        ///     An optional message correlation Id.
        /// </param>
		public DockerLogEntry(DockerLogStreamType streamType, ByteString data, string correlationId = null)
            : base(correlationId)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			StreamType = streamType;
			Data = data;
		}

		/// <summary>
		///     A <see cref="DockerLogStreamType"/> identifying the type of stream (e.g. STDOUT / STDERR) represented by the <see cref="DockerLogEntry"/>.
		/// </summary>
		public DockerLogStreamType StreamType { get; }

		/// <summary>
		///     The log entry data.
		/// </summary>
		public ByteString Data { get; }

        /// <summary>
        ///     The log entry text.
        /// </summary>
        public string Text => Data.ToString(DefaultEncoding);

		/// <summary>
		///     Read a <see cref="DockerLogEntry"/> from the specified data.
		/// </summary>
		/// <param name="data">
		///     A <see cref="ByteString"/> containing the data.
		/// </param>
        /// <param name="correlationId">
        ///     An optional message-correlation Id.
        /// </param>
		/// <returns>
		///     The log entry (or <c>null</c>, if not enough data is available) and any remaining data.
		/// </returns>
		public static (DockerLogEntry logEntry, ByteString remainingData) ReadFrom(ByteString data, string correlationId = null)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var (streamType, logEntryLength, isValidHeader) = ReadLogEntryHeader(data);
            if (!isValidHeader || data.Count < HeaderLength + logEntryLength)
				return (logEntry: null, remainingData: data); // Not enough data.

			ByteString logEntryData = data.Drop(HeaderLength).Take(logEntryLength);

			return (
				logEntry: new DockerLogEntry(streamType, logEntryData, correlationId),
				remainingData: data.Drop(HeaderLength + logEntryLength)
			);
		}

		/// <summary>
		///     Read a docker log-entry header from the specified data.
		/// </summary>
		/// <param name="data">
		///     A <see cref="ByteString"/> representing the data.
		/// </param>
		/// <returns>
		///     The log entry stream type and length, and a value indicating whether a valid header was found in the data.
		/// </returns>
		/// <remarks>
		///     Each log entry has a header in the following format: [STREAM_TYPE, 0, 0, 0, SIZE1, SIZE2, SIZE3, SIZE4]
		///     
		///     Where STREAM_TYPE is a value from DockerLogEntryStreamType, and SIZE1-SIZE4 make up a big-endian Int32
		///     representing the length of the log entry data (excluding the header).
		///     
		///     See <see href="https://docs.docker.com/engine/api/v1.24/#brief-introduction"/> for further details.
		/// </remarks>
		public static (DockerLogStreamType streamType, int length, bool isValid) ReadLogEntryHeader(ByteString data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (data.Count < HeaderLength)
				return (streamType: DockerLogStreamType.Unknown, length: -1, isValid: false);

			byte[] header = data.Slice(0, HeaderLength).ToArray();

            // Switch to little-endian.
            Array.Reverse(header, index: 4, length: 4);

			return (
				streamType: (DockerLogStreamType)header[0],
				length: BitConverter.ToInt32(header, startIndex: 4),
                isValid: true
			);
		}
	}
}
