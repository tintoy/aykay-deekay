using System;

namespace AKDK.Messages
{
    /// <summary>
    ///		The base class for messages representing requests to the Docker API.
    /// </summary>
    public abstract class Request
        : CorrelatedMessage
    {
        /// <summary>
        ///		Create a new <see cref="Request"/> message.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id.
        /// </param>
        protected Request(string correlationId)
            : base(correlationId)
        {
        }

        /// <summary>
        ///     A short name for the operation represented by the request.
        /// </summary>
        public abstract string OperationName { get; }

        /// <summary>
        ///     Create a string representation of the request.
        /// </summary>
        /// <returns>
        ///     A string in the format "TypeName(CorrelationId): OperationName".
        /// </returns>
        public override string ToString() => String.Format("{0}({1}): {1}", GetType().Name, CorrelationId, OperationName);
    }
}
