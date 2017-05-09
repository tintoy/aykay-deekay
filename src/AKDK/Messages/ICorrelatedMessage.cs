namespace AKDK.Messages
{
    /// <summary>
    ///     Represents a correlated message.
    /// </summary>
    public interface ICorrelatedMessage
    {
        /// <summary>
        ///		The message correlation Id.
        /// </summary>
        string CorrelationId { get; }
    }
}
