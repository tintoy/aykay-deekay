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
		}
    }
}
