using Akka.Actor;
using AKDK.Actors;
using System;
using System.Collections.Immutable;

namespace AKDK.Examples.Orchestration.Actors
{
    using Messages;

    /// <summary>
    ///     Actor that manages the job store event bus.
    /// </summary>
    class JobStoreEvents
        : EventBusActor<JobStoreEvent>
    {
        /// <summary>
        ///     The default name for instances of the <see cref="JobStoreEvents"/> actor.
        /// </summary>
        public static readonly string ActorName = "job-store-events";

        /// <summary>
        ///     All top-level job store event types.
        /// </summary>
        public static ImmutableList<Type> AllJobStoreEventTypes = ImmutableList.Create(typeof(JobStoreEvent));

        /// <summary>
        ///     Create a new <see cref="JobStoreEvents"/> actor.
        /// </summary>
        public JobStoreEvents()
        {
        }

        /// <summary>
        ///     All event types known to the event bus.
        /// </summary>
        protected override ImmutableList<Type> AllEventTypes => AllJobStoreEventTypes;

        /// <summary>
        ///     Generate <see cref="Props"/> to create an instance of the <see cref="JobStoreEvents"/> actor.
        /// </summary>
        public static Props Create() => Props.Create<JobStoreEvents>();
    }
}
