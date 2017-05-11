using System;
using System.Collections.Generic;
using System.Text;

namespace AKDK.Messages
{
    using Actors;

    /// <summary>
    ///     Instruct a <see cref="Client"/> to cancel a request.
    /// </summary>
    public class CancelRequest
        : CorrelatedMessage
    {
        /// <summary>
        ///     Create a new <see cref="CancelRequest"/> correlation Id.
        /// </summary>
        /// <param name="correlationId">
        ///     The correlation Id of the request to cancel.
        /// </param>
        public CancelRequest(string correlationId)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(correlationId)}.", nameof(correlationId));
        }
    }
}
