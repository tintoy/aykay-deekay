using System;

namespace AKDK.Messages
{
    /// <summary>
    ///		The base class for messages with a correlation Id.
    /// </summary>
    public abstract class CorrelatedMessage
    {
        /// <summary>
        ///		Create a new <see cref="CorrelatedMessage"/>.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id.
        /// </param>
        protected CorrelatedMessage(string correlationId)
        {
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }

        /// <summary>
        ///		The message correlation Id.
        /// </summary>
        public string CorrelationId { get; }
    }
}
