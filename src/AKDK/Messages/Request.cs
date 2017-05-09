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
    }
}
