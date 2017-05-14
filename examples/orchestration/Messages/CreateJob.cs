using System;

namespace AKDK.Examples.Orchestration.Messages
{
    using AKDK.Messages;

    /// <summary>
    ///     Request to create a new job.
    /// </summary>
    public class CreateJob
        : CorrelatedMessage
    {
        /// <summary>
        ///     Create a new <see cref="CreateJob"/> message.
        /// </summary>
        /// <param name="targetUrl">
        ///     The target URL to fetch.
        /// </param>
        /// <param name="correlationId">
        ///     An optional message-correlation Id.
        /// </param>
        public CreateJob(Uri targetUrl, string correlationId = null)
            : base(correlationId)
        {
            if (targetUrl == null)
                throw new ArgumentNullException(nameof(targetUrl));

            if (targetUrl.Scheme != "http" && targetUrl.Scheme != "https")
                throw new ArgumentException($"Unsupported URI scheme '{targetUrl.Scheme}' (only 'http' and 'https' are supported).", nameof(targetUrl));

            TargetUrl = targetUrl;
        }

        /// <summary>
        ///     An optional message-correlation Id.
        /// </summary>
        public Uri TargetUrl { get; }
    }
}
