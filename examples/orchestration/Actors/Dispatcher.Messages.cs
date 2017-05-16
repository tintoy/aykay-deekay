using AKDK.Messages;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor that dispatches jobs and tracks their state.
    /// </summary>
    public partial class Dispatcher
    {
        /// <summary>
        ///     Ask the <see cref="Dispatcher"/> to notify the sender then all active jobs have been completed.
        /// </summary>
        public class NotifyWhenAllJobsCompleted
        {
            /// <summary>
            ///     Create a new <see cref="NotifyWhenAllJobsCompleted"/> message.
            /// </summary>
            /// <param name="correlationId">
            ///     An optional message correlation Id.
            /// </param>
            public NotifyWhenAllJobsCompleted()
            {
            }
        }

        /// <summary>
        ///     Notification from the <see cref="Dispatcher"/> that all active jobs have been completed.
        /// </summary>
        public class AllJobsCompleted
        {
            /// <summary>
            ///     Create a new <see cref="AllJobsCompleted"/> message.
            /// </summary>
            /// <param name="correlationId">
            ///     An optional message correlation Id.
            /// </param>
            public AllJobsCompleted()
            {
            }
        }
    }
}
