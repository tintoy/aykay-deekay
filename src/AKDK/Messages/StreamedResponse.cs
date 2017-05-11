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
        /// <param name="format">
        ///     The response stream format.
        /// </param>
        public StreamedResponse(string correlationId, Stream responseStream, StreamedResponseFormat format)
            : base(correlationId)
        {
            if (responseStream == null)
                throw new ArgumentNullException(nameof(responseStream));

            ResponseStream = responseStream;
            Format = format;
        }

        /// <summary>
        ///     The response stream.
        /// </summary>
        public Stream ResponseStream { get; }

        /// <summary>
        ///     The response stream format.
        /// </summary>
        public StreamedResponseFormat Format { get; }
    }
}
