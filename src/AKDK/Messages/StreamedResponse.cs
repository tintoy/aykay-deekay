using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        public StreamedResponse(string correlationId, Stream responseStream)
            : base(correlationId)
        {
            if (responseStream == null)
                throw new ArgumentNullException(nameof(responseStream));

            ResponseStream = responseStream;
        }

        /// <summary>
        ///     The response stream.
        /// </summary>
        public Stream ResponseStream { get; }
    }
}
