using System;

namespace AKDK.Messages
{

    /// <summary>
    ///		The base class for messages with a correlation Id.
    /// </summary>
    public abstract class CorrelatedMessage
        : ICorrelatedMessage
    {
        /// <summary>
        ///     Generate a message correlation Id.
        /// </summary>
        /// <returns>
        ///     The new message correlation Id.
        /// </returns>
        public static string NewCorrelationId() => Guid.NewGuid().ToString("N"); // Just digits, no dashes or braces.

        /// <summary>
        ///		Create a new <see cref="CorrelatedMessage"/>.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id.
        /// </param>
        protected CorrelatedMessage(string correlationId)
        {
            CorrelationId = correlationId ?? NewCorrelationId();
        }

        /// <summary>
        ///		The message correlation Id.
        /// </summary>
        public string CorrelationId { get; }
    }
}
