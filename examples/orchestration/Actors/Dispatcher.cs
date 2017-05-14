using Akka.Actor;
using AKDK.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace AKDK.Examples.Orchestration.Actors
{
    using Messages;

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

        readonly IActorRef _jobStore;

        public Dispatcher(IActorRef jobStore)
        {
            if (jobStore == null)
                throw new ArgumentNullException(nameof(jobStore));

            Receive<JobCreated>(jobCreated =>
            {
                // TODO: Schedule job.
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            Context.Watch(_jobStore);
            _jobStore.Tell(
                new EventBusActor.Subscribe(Self,
                    eventTypes: new Type[]
                    {
                        typeof(JobCreated)
                    }
                )
            );
        }
    }
}
