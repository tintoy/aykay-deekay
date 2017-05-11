namespace AKDK.Messages
{
    /// <summary>
    ///     Well-known streamed response formats.
    /// </summary>
    public enum StreamedResponseFormat
    {
        /// <summary>
        ///     An unknown response type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     A Docker log stream.
        /// </summary>
        Log = 1,

        /// <summary>
        ///     A Docker event stream.
        /// </summary>
        Events = 2
    }
}
