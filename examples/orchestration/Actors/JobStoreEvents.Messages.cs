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
    }
}
