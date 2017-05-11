namespace AKDK.Messages
{
    /// <summary>
    ///		Represents the end of a log.
    /// </summary>
    public class EndOfLog
        : CorrelatedMessage
    {
        /// <summary>
        ///		Create a new <see cref="EndOfLog"/> message.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id.
        /// </param>
        public EndOfLog(string correlationId)
            : base(correlationId)
        {
        }
    }
}
