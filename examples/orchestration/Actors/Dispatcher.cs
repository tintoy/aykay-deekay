using Akka.Actor;
using AKDK.Actors;
using System;
using System.Collections.Generic;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor that dispatches jobs and tracks their state.
    /// </summary>
    public class Dispatcher
        : ReceiveActor
    {
        /// <summary>
        ///     Container-management actors, keyed by job Id.
        /// </summary>
        readonly Dictionary<int, IActorRef> _jobContainers = new Dictionary<int, IActorRef>();

        /// <summary>
        ///     A reference to the <see cref="JobStore"/> actor.
        /// </summary>
        readonly IActorRef                  _jobStore;

        /// <summary>
        ///     A reference to the <see cref="Launcher"/> actor.
        /// </summary>
        readonly IActorRef                  _launcher;

        /// <summary>
        ///     Create a new <see cref="Dispatcher"/>.
        /// </summary>
        /// <param name="jobStore">
        ///     A reference to the <see cref="JobStore"/> actor.
        /// </param>
        /// <param name="launcher">
        ///     A reference to the <see cref="Launcher"/> actor.
        /// </param>
        public Dispatcher(IActorRef jobStore, IActorRef launcher)
        {
            if (jobStore == null)
                throw new ArgumentNullException(nameof(jobStore));

            if (launcher == null)
                throw new ArgumentNullException(nameof(launcher));

            _jobStore = jobStore;
            _launcher = launcher;

            Receive<JobStoreEvents.JobCreated>(jobCreated =>
            {
                // TODO: Start job using Launcher.
            });
        }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            _jobStore.Tell(
                new EventBusActor.Subscribe(Self, eventTypes: new Type[]
                {
                    typeof(JobStoreEvents.JobCreated)
                })
            );
            Context.Watch(_jobStore);
        }
    }
}
