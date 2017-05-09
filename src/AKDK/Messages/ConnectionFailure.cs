using Akka.Actor;
using System;

namespace AKDK.Messages
{
    /// <summary>
    ///     Message indicating a failure to connect to the Docker API.
    /// </summary>
    public class ConnectionFailure
        : Failure, ICorrelatedMessage
    {
        /// <summary>
        ///     Create a new <see cref="ConnectionFailure"/> message.
        /// </summary>
        /// <param name="exception">
        ///     The exception that was raised due to connection failure.
        /// </param>
        /// <param name="correlationId">
        ///     The message correlation Id.
        /// </param>
        public ConnectionFailure(Exception exception, string correlationId)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (String.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(correlationId)}.", nameof(correlationId));

            CorrelationId = correlationId;
            Exception = exception;
        }

        /// <summary>
        ///     The message correlation Id.
        /// </summary>
        public string CorrelationId { get; }
    }
}
