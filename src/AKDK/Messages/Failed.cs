using System;

namespace AKDK.Messages
{
    /// <summary>
    ///		Represents a Docker API operation that failed.
    /// </summary>
    public class Failed
        : Response
    {
        /// <summary>
        ///		Create a new <see cref="Failed"/> message.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that was assigned to the original request message.
        /// </param>
        /// <param name="operationName">
        ///		The name of the operation that failed.
        /// </param>
        /// <param name="exception">
        ///		An <see cref="System.Exception"/> representing the failed exception.
        /// </param>
        public Failed(string correlationId, string operationName, Exception exception)
            : base(correlationId)
        {
            OperationName = operationName;
            Exception = exception;
        }

        /// <summary>
        ///		The name of the operation that failed.
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        ///		An <see cref="System.Exception"/> representing the failed exception.
        /// </summary>
        public Exception Exception { get; }
    }
}
