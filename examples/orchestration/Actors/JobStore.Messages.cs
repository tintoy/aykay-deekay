using System;

namespace AKDK.Examples.Orchestration.Actors
{
    using Messages;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    /// <summary>
    ///     Actor used to persist information about active jobs.
    /// </summary>
    public partial class JobStore
    {
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

        public class UpdateJob
            : CorrelatedMessage
        {
            public UpdateJob(int jobId, JobStatus status, IEnumerable<string> appendMessages = null, string content = null, string correlationId = null)
                : base(correlationId)
            {
                JobId = jobId;
                Status = status;
                AppendMessages = appendMessages != null ? ImmutableList.CreateRange(appendMessages) : ImmutableList<string>.Empty;
                Content = content;
            }

            public int JobId { get; }
            public JobStatus Status { get; }
            public ImmutableList<string> AppendMessages { get; }
            public string Content { get; }
        }
    }
}
