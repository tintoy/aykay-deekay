using System;
using System.IO;

namespace AKDK.Messages
{
    /// <summary>
    ///     Docker API response indicating that the response payload will be streamed.
    /// </summary>
    public class StreamedResponse
        : Response
    {
        /// <summary>
        ///     Create a new <see cref="StreamedResponse"/> message.
        /// </summary>
        /// <param name="correlationId">
        ///     The message correlationId.
        /// </param>
        /// <param name="responseStream">
        ///     The response stream.
        /// </param>
        /// <param name="isLog">
        ///     Is the stream in Docker log format?
        /// </param>
        public StreamedResponse(string correlationId, Stream responseStream, bool isLog = false)
            : base(correlationId)
        {
            if (responseStream == null)
                throw new ArgumentNullException(nameof(responseStream));

            ResponseStream = responseStream;
            IsLog = isLog;
        }

        /// <summary>
        ///     The response stream.
        /// </summary>
        public Stream ResponseStream { get; }

        /// <summary>
        ///     Is the stream in Docker log format?
        /// </summary>
        public bool IsLog { get; }
    }
}
