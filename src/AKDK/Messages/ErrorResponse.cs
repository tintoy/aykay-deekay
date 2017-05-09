using System;

namespace AKDK.Messages
{
    /// <summary>
    ///		Represents an error response from the Docker API.
    /// </summary>
    public class ErrorResponse
        : Response
    {
        /// <summary>
        ///		Create a new <see cref="ErrorResponse"/> message.
        /// </summary>
        /// <param name="request">
        ///		The request that the response relates to.
        /// </param>
        /// <param name="exception">
        ///		An exception representing the error.
        /// </param>
        public ErrorResponse(Request request, Exception exception)
            : base(request.CorrelationId)
        {
            Request = request;
            Exception = exception;
        }

        /// <summary>
        ///		The request that the response relates to.
        /// </summary>
        public Request Request { get; }

        /// <summary>
        ///		An exception representing the error.
        /// </summary>
        public Exception Exception { get; }
    }
}
