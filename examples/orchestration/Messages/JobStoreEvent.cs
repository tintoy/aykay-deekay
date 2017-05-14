using System;

namespace AKDK.Examples.Orchestration.Messages
{
    using AKDK.Messages;

    /// <summary>
    ///     The base class for job store events.
    /// </summary>
    public abstract class JobStoreEvent
        : CorrelatedMessage
    {
        /// <summary>
        ///     Create a new <see cref="JobStoreEvent"/>.
        /// </summary>
        /// <param name="correlationId">
        ///     The event's message correlation Id.
        /// </param>
        protected JobStoreEvent(string correlationId)
            : base(correlationId)
        {
        }
    }

    /// <summary>
    ///     Event raised when a job is created.
    /// </summary>
    public class JobCreated
        : JobStoreEvent
    {
        /// <summary>
        ///     Create a new <see cref="JobCreated"/> event.
        /// </summary>
        /// <param name="correlationId">
        ///     The event message correlation Id.
        /// </param>
        /// <param name="jobId">
        ///     The Id of the newly-created job.
        /// </param>
        /// <param name="targetUrl">
        ///     The target URL for the newly-created job.
        /// </param>
        public JobCreated(string correlationId, int jobId, Uri targetUrl)
            : base(correlationId)
        {
            if (jobId < 1)
                throw new ArgumentOutOfRangeException(nameof(jobId), jobId, "Job Id cannot be less than 1.");

            if (targetUrl == null)
                throw new ArgumentNullException(nameof(targetUrl));

            JobId = jobId;
            TargetUrl = targetUrl;
        }

        /// <summary>
        ///     The Id of the newly-created job.
        /// </summary>
        public int JobId { get; }

        /// <summary>
        ///     The target URL for the newly-created job.
        /// </summary>
        public Uri TargetUrl { get; }
    }
}
