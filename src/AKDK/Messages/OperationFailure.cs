using System;

namespace AKDK.Messages
{
    /// <summary>
    ///		Represents a Docker API operation that failed.
    /// </summary>
    public class OperationFailure
        : Response
    {
        /// <summary>
        ///		Create a new <see cref="OperationFailure"/> message.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that was assigned to the original request message.
        /// </param>
        /// <param name="operationName">
        ///		The name of the operation that failed.
        /// </param>
        /// <param name="reason">
        ///		An <see cref="Exception"/> representing the reason for the failure.
        /// </param>
        public OperationFailure(string correlationId, string operationName, Exception reason)
            : base(correlationId)
        {
            OperationName = operationName;
            Reason = reason;
        }

        /// <summary>
        ///		The name of the operation that failed.
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        ///		An <see cref="Exception"/> representing the reason for the failure.
        /// </summary>
        public Exception Reason { get; }
    }
}
