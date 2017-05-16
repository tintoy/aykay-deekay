using AKDK.Messages;
using System;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor that manages the job store event bus.
    /// </summary>
    partial class JobStoreEvents
    {
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
            /// <param name="job">
            ///     The newly-created job.
            /// </param>
            public JobCreated(string correlationId, Job job)
                : base(correlationId)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));

                Job = job;
            }

            /// <summary>
            ///     The newly-created job.
            /// </summary>
            public Job Job { get; }
        }

        /// <summary>
        ///     Event raised when a job is started.
        /// </summary>
        public class JobStarted
            : JobStoreEvent
        {
            /// <summary>
            ///     Create a new <see cref="JobStarted"/> event.
            /// </summary>
            /// <param name="correlationId">
            ///     The event message correlation Id.
            /// </param>
            /// <param name="job">
            ///     The job that was started.
            /// </param>
            public JobStarted(string correlationId, Job job)
                : base(correlationId)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));

                Job = job;
            }

            /// <summary>
            ///     The job that was started.
            /// </summary>
            public Job Job { get; }
        }

        /// <summary>
        ///     Event raised when a job succeeded.
        /// </summary>
        public class JobSucceeded
            : JobStoreEvent
        {
            /// <summary>
            ///     Create a new <see cref="JobSucceeded"/> event.
            /// </summary>
            /// <param name="correlationId">
            ///     The event message correlation Id.
            /// </param>
            /// <param name="job">
            ///     The job that succeeded.
            /// </param>
            public JobSucceeded(string correlationId, Job job)
                : base(correlationId)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));

                Job = job;
            }

            /// <summary>
            ///     The job that was started.
            /// </summary>
            public Job Job { get; }
        }

        /// <summary>
        ///     Event raised when a job has failed.
        /// </summary>
        public class JobFailed
            : JobStoreEvent
        {
            /// <summary>
            ///     Create a new <see cref="JobFailed"/> event.
            /// </summary>
            /// <param name="correlationId">
            ///     The event message correlation Id.
            /// </param>
            /// <param name="job">
            ///     The job that failed.
            /// </param>
            public JobFailed(string correlationId, Job job)
                : base(correlationId)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));

                Job = job;
            }

            /// <summary>
            ///     The job that was started.
            /// </summary>
            public Job Job { get; }
        }

        /// <summary>
        ///     Event raised when a job is completed.
        /// </summary>
        public class JobCompleted
            : JobStoreEvent
        {
            /// <summary>
            ///     Create a new <see cref="JobCompleted"/> event.
            /// </summary>
            /// <param name="correlationId">
            ///     The event message correlation Id.
            /// </param>
            /// <param name="job">
            ///     The job that was started.
            /// </param>
            public JobCompleted(string correlationId, Job job)
                : base(correlationId)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));

                Job = job;
            }

            /// <summary>
            ///     The job that was started.
            /// </summary>
            public Job Job { get; }
        }
    }
}
