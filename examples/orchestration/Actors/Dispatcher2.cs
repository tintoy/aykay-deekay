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
        ///     References to <see cref="Job"/>s, keyed by job Id.
        /// </summary>
        readonly Dictionary<int, Job>       _jobs = new Dictionary<int, Job>();

        /// <summary>
        ///     Jobs, keyed by references to <see cref="JobWorker"/> actors.
        /// </summary>
        readonly Dictionary<IActorRef, Job> _jobWorkers = new Dictionary<IActorRef, Job>();
        
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
                Job job = jobCreated.Job;
                _jobs.Add(job.Id, job);

                DirectoryInfo jobStateDirectory = _stateDirectory.CreateSubdirectory($"job-{jobCreated.Job.Id}");

                IActorRef worker = Context.ActorOf(Props.Create(
                    () => new JobWorker(_launcher, _harvester)
                ));
                Context.Watch(worker);
                _jobWorkers.Add(worker, job);

                worker.Tell(
                    new JobWorker.ExecuteJob(job,
                        stateDirectory: jobStateDirectory,
                        correlationId: jobCreated.CorrelationId
                    )
                );
            });
            Receive<JobWorker.JobExecuting>(jobExecuting =>
            {
                Job updatedJob = jobExecuting.Job;
                
                Job existingJob;
                if (!_jobs.TryGetValue(updatedJob.Id, out existingJob))
                {
                    Log.Warning("Received unexpected JobExecuting ({JobStatus}) notification for job {JobId} from {Worker}",
                        updatedJob.Status,
                        updatedJob.Id,
                        Sender
                    );

                    return;
                }

                _jobs[updatedJob.Id] = jobExecuting.Job;

                _jobStore.Tell(new JobStore.UpdateJob(updatedJob.Id,
                    status: updatedJob.Status,
                    appendMessages: updatedJob.Messages.Skip(existingJob.Messages.Count),
                    correlationId: jobExecuting.CorrelationId
                ));
            });
            Receive<JobWorker.JobExecuted>(jobExecuted =>
            {
                Job updatedJob = jobExecuted.Job;
                
                Job existingJob;
                if (!_jobs.TryGetValue(updatedJob.Id, out existingJob))
                {
                    Log.Warning("Received unexpected JobExecuted ({JobStatus}) notification for job {JobId} from {Worker}",
                        updatedJob.Status,
                        updatedJob.Id,
                        Sender
                    );

                    return;
                }

                _jobs.Remove(existingJob.Id);
                _jobWorkers.Remove(Sender);
                _jobStore.Tell(new JobStore.UpdateJob(updatedJob.Id,
                    status: updatedJob.Status,
                    content: updatedJob.Content,
                    appendMessages: updatedJob.Messages.Skip(existingJob.Messages.Count),
                    correlationId: jobExecuted.CorrelationId
                ));
            });
            Receive<JobWorker.LaunchTimeout>(launchTimeout =>
            {
                if (!_jobWorkers.ContainsKey(Sender))
                {
                    Log.Warning("Received unexpected LaunchTimeout notification from worker {Worker} (no job associated with this worker).", Sender);

                    return;
                }

                Job existingJob;
                if (!_jobs.TryGetValue(launchTimeout.JobId, out existingJob))
                {
                    Log.Warning("Received unexpected LaunchTimeout notification from worker {Worker} (the dispatcher has no knowledge of job {JobId}).", Sender, launchTimeout.JobId);

                    return;
                }

                _jobStore.Tell(new JobStore.UpdateJob(existingJob.Id,
                    status: JobStatus.Failed,
                    appendMessages: new string[] { "Timed out waiting for the job process to launch." }
                ));

                _jobs.Remove(existingJob.Id);
                _jobWorkers.Remove(Sender);
            });
            Receive<JobWorker.HarvestTimeout>(harvestTimeout =>
            {
                if (!_jobWorkers.ContainsKey(Sender))
                {
                    Log.Warning("Received unexpected HarvestTimeout notification from worker {Worker} (no job associated with this worker).", Sender);

                    return;
                }

                Job existingJob;
                if (!_jobs.TryGetValue(harvestTimeout.JobId, out existingJob))
                {
                    Log.Warning("Received unexpected HarvestTimeout notification from worker {Worker} (the dispatcher has no knowledge of job {JobId}).", Sender, harvestTimeout.JobId);

                    return;
                }

                _jobStore.Tell(new JobStore.UpdateJob(existingJob.Id,
                    status: JobStatus.Failed,
                    appendMessages: new string[] { "Timed out waiting to harvest the job results." }
                ));

                _jobs.Remove(existingJob.Id);
                _jobWorkers.Remove(Sender);
            });
            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef == _jobStore)
                {
                    Log.Warning("Job store has terminated; the Dispatcher2 cannot continue.");

                    Unhandled(terminated); // Results in DeathPactException.

                    return;
                }

                if (terminated.ActorRef == _harvester)
                {
                    Log.Warning("Harvester has terminated; the Dispatcher2 cannot continue.");

                    Unhandled(terminated); // Results in DeathPactException.

                    return;
                }

                Job existingJob;
                if (_jobWorkers.TryGetValue(terminated.ActorRef, out existingJob))
                {
                    Log.Warning("Worker for job {JobId} ({Worker}) terminated unexpectedly; the job will be marked as failed.",
                        existingJob.Id,
                        terminated.ActorRef
                    );

                    _jobWorkers.Remove(terminated.ActorRef);
                    _jobs.Remove(existingJob.Id);
                    _jobStore.Tell(
                        new JobStore.UpdateJob(existingJob.Id, JobStatus.Failed, appendMessages: new string[]
                        {
                            "The worker actor terminated unexpectedly while processing the job."
                        })
                    );

                    return;
                }
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

        /// <summary>
        ///     Generate <see cref="Props"/> to create a new <see cref="Dispatcher2"/>.
        /// </summary>
        /// <param name="stateDirectory">
        ///     The root state directory.
        /// </param>
        /// <param name="jobStore">
        ///     A reference to the <see cref="JobStore"/> actor.
        /// </param>
        /// <param name="launcher">
        ///     A reference to the <see cref="Launcher"/> actor.
        /// </param>
        /// <returns>
        ///     The configured <see cref="Props"/>.
        /// </returns>
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
