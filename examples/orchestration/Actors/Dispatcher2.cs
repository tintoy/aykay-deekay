using Akka.Actor;
using AKDK.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections.Immutable;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor that dispatches jobs and tracks their state.
    /// </summary>
    public partial class Dispatcher2
        : ReceiveActorEx, IWithUnboundedStash
    {
        /// <summary>
        ///     The default name for instances of the <see cref="Dispatcher2"/> actor.
        /// </summary>
        public static readonly string ActorName = "Dispatcher2";
        
        /// <summary>
        ///     The directory containing job-specific state directories.
        /// </summary>
        readonly DirectoryInfo              _stateDirectory;

        /// <summary>
        ///     A reference to the <see cref="JobStore"/> actor.
        /// </summary>
        readonly IActorRef                  _jobStore;

        /// <summary>
        ///     A reference to the <see cref="Launcher"/> actor.
        /// </summary>
        readonly IActorRef                  _launcher;

        /// <summary>
        ///     A reference to the <see cref="Harvester2"/> actor.
        /// </summary>
        IActorRef                           _harvester;

        /// <summary>
        ///     Create a new <see cref="Dispatcher2"/>.
        /// </summary>
        /// <param name="stateDirectory">
        ///     The root directory for state directories that will be mounted into containers.
        /// </param>
        /// <param name="jobStore">
        ///     A reference to the <see cref="JobStore"/> actor.
        /// </param>
        /// <param name="launcher">
        ///     A reference to the <see cref="Launcher"/> actor.
        /// </param>
        public Dispatcher2(DirectoryInfo stateDirectory, IActorRef jobStore, IActorRef launcher)
        {
            if (stateDirectory == null)
                throw new ArgumentNullException(nameof(stateDirectory));

            if (jobStore == null)
                throw new ArgumentNullException(nameof(jobStore));

            if (launcher == null)
                throw new ArgumentNullException(nameof(launcher));

            _stateDirectory = stateDirectory;
            _jobStore = jobStore;
            _launcher = launcher;
        }

        /// <summary>
        ///     The actor's message stash.
        /// </summary>
        public IStash Stash { get; set; }

        /// <summary>
        ///     Called when the actor is initialising.
        /// </summary>
        void Initializing()
        {
            // AF: There is still a race condition for startup; if the call to Thread.Sleep in Program.cs is removed, the system does not process the job.
            // TODO: Figure out why (probably when I get back - out of time for now).

            Receive<EventBusActor.Subscribed>(subscribed =>
            {
                Become(Ready);
            });
            Receive<object>(_ =>
            {
                // Wait till we're ready.
                Stash.Stash();
            });
        }

        /// <summary>
        ///     Called when the Dispatcher2 is ready to handle requests.
        /// </summary>
        void Ready()
        {
            Stash.UnstashAll();

            Receive<JobStoreEvents.JobCreated>(jobCreated =>
            {
                DirectoryInfo jobStateDirectory = _stateDirectory.CreateSubdirectory($"job-{jobCreated.Job.Id}");

                // TODO: Create JobWorker and tell it to execute the job.
            });
            Receive<JobWorker.JobExecuting>(jobExecuting =>
            {
                // TODO: Update status for target job.
            });
            Receive<JobWorker.JobExecuted>(jobExecuted =>
            {
                // TODO: Update status and clear state for target job.
            });
            Receive<JobWorker.LaunchTimeout>(launchTimeout =>
            {
                // TODO: Update status and clear state for target job.
            });
            Receive<JobWorker.HarvestTimeout>(harvestTimeout =>
            {
                // TODO: Update status and clear state for target job.
            });
            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef == _jobStore)
                {
                    Log.Warning("Job store has terminated; the Dispatcher2 cannot continue.");

                    Unhandled(terminated); // Results in DeathPactException.

                    return;
                }

                // TODO: Handle termination of other actors.

                // TODO: Update status and clear state for target job (if appropriate).
            });
        }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            if (!_stateDirectory.Exists)
                _stateDirectory.Create();

            _harvester = Context.ActorOf(Props.Create(
                () => new Harvester2()
            ));
            Context.Watch(_harvester);

            _jobStore.Tell(
                new EventBusActor.Subscribe(Self, eventTypes: new Type[]
                {
                    typeof(JobStoreEvents.JobCreated),
                    typeof(JobStoreEvents.JobCompleted)
                })
            );
            Context.Watch(_jobStore);

            Become(Initializing);
        }

        public static Props Create(DirectoryInfo stateDirectory, IActorRef jobStore, IActorRef launcher)
        {
            if (stateDirectory == null)
                throw new ArgumentNullException(nameof(stateDirectory));

            if (jobStore == null)
                throw new ArgumentNullException(nameof(jobStore));

            if (launcher == null)
                throw new ArgumentNullException(nameof(launcher));

            return Props.Create(
                () => new Dispatcher2(stateDirectory, jobStore, launcher)
            );
        }
    }
}
