using System;

namespace AKDK.Messages
{
    /// <summary>
    ///		The base class for messages that represent responses from the Docker API.
    /// </summary>
    public abstract class Response
        : CorrelatedMessage
    {
        /// <summary>
        ///		Create a new <see cref="Response"/> message.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id.
        /// </param>
        protected Response(string correlationId)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException($"Response messages must have a correlation Id.", nameof(correlationId));
        }

        /// <summary>
        ///     Create a string representation of the request.
        /// </summary>
        /// <returns>
        ///     A string in the format "TypeName(CorrelationId)".
        /// </returns>
        public override string ToString() => String.Format("{0}({1})", GetType().Name, CorrelationId);
    }
}
