using Akka.Actor;
using AKDK.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections.Immutable;

namespace AKDK.Examples.Orchestration.Actors
{
    // TODO: Need to stop relying on correlation Ids for process lookup (this is messy, implicit, and unreliable).
    //       Instead, either use container Id or sender's ActorRef.

    /// <summary>
    ///     Actor that dispatches jobs and tracks their state.
    /// </summary>
    public partial class Dispatcher
        : ReceiveActorEx, IWithUnboundedStash
    {
        /// <summary>
        ///     The default name for instances of the <see cref="Dispatcher"/> actor.
        /// </summary>
        public static readonly string ActorName = "dispatcher";

        /// <summary>
        ///     Active jobs, keyed by message correlation Id.
        /// </summary>
        readonly Dictionary<string, Job>    _activeJobs = new Dictionary<string, Job>();

        /// <summary>
        ///     <see cref="Process"/>es for active jobs, keyed by job Id.
        /// </summary>
        readonly Dictionary<int, IActorRef> _jobProcesses = new Dictionary<int, IActorRef>();

        /// <summary>
        ///     Actors to notify when all active job processes have been completed.
        /// </summary>
        readonly Queue<IActorRef>           _notifyJobProcessCompletion = new Queue<IActorRef>();

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
        ///     Create a new <see cref="Dispatcher"/>.
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
        public Dispatcher(DirectoryInfo stateDirectory, IActorRef jobStore, IActorRef launcher)
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
        ///     Called when the dispatcher is ready to handle requests.
        /// </summary>
        void Ready()
        {
            Stash.UnstashAll();

            Receive<JobStoreEvents.JobCreated>(jobCreated =>
            {
                DirectoryInfo jobStateDirectory = _stateDirectory.CreateSubdirectory($"job-{jobCreated.Job.Id}");

                // TODO: Create JobWorker and tell it to execute the job.

                Log.Info("Dispatcher is creating fetcher process for job {0} (host-side state directory is '{1}').",
                    jobCreated.Job.Id,
                    jobStateDirectory.FullName
                );

                _launcher.Tell(new Launcher.CreateProcess(
                    owner: Self,
                    imageName: "fetcher",
                    environmentVariables: new Dictionary<string, string>
                    {
                        ["TARGET_URL"] = jobCreated.Job.TargetUrl.AbsoluteUri
                    }.ToImmutableDictionary(),
                    binds: new Dictionary<string, string>
                    {
                        [jobStateDirectory.FullName] = "/root/state"
                    }.ToImmutableDictionary(),
                    correlationId: jobCreated.CorrelationId
                ));
                _activeJobs.Add(jobCreated.CorrelationId, jobCreated.Job);
            });
            Receive<Launcher.ProcessCreated>(processCreated =>
            {
                Job activeJob;
                if (!_activeJobs.TryGetValue(processCreated.CorrelationId, out activeJob))
                {
                    Log.Warning("Received unexpected ProcessCreated notification from '{0}' (CorrelationId = {1}).",
                        Sender,
                        processCreated.CorrelationId
                    );

                    Unhandled(processCreated);

                    return;
                }

                Context.Watch(processCreated.Process);
                _jobProcesses.Add(activeJob.Id, processCreated.Process);

                Log.Info("Starting container '{0}' for job '{1}'...", processCreated.ContainerId, activeJob.Id);

                processCreated.Process.Tell(
                    new Process.Start(processCreated.CorrelationId)
                );
            });
            Receive<Process.Started>(processStarted =>
            {
                Job activeJob;
                if (!_activeJobs.TryGetValue(processStarted.CorrelationId, out activeJob))
                {
                    Log.Warning("Received unexpected ProcessStarted notification from '{0}' (CorrelationId = {1}).",
                        Sender,
                        processStarted.CorrelationId
                    );

                    Unhandled(processStarted);

                    return;
                }

                _jobStore.Tell(new JobStore.UpdateJob(activeJob.Id,
                    status: JobStatus.Active,
                    appendMessages: new string[]
                    {
                        "Job started."
                    }
                ));
            });
            Receive<Process.Exited>(processExited =>
            {
                Job activeJob;
                if (!_activeJobs.TryGetValue(processExited.CorrelationId, out activeJob))
                {
                    Log.Warning("Received unexpected ProcessExited notification from '{0}' (CorrelationId = {1}).",
                        Sender,
                        processExited.CorrelationId
                    );

                    Unhandled(processExited);

                    return;
                }

                _jobStore.Tell(new JobStore.UpdateJob(activeJob.Id,
                    status: processExited.ExitCode == 0 ? JobStatus.Succeeded : JobStatus.Failed,
                    appendMessages: new string[]
                    {
                        $"Job completed with exit code {processExited.ExitCode}."
                    }
                ));
            });
            Receive<JobStoreEvents.JobCompleted>(jobCompleted =>
            {
                // TODO: Stop relying on correlation Id (redesign active-job tables around Job Id and Sender).
                Job activeJob = _activeJobs.Values.FirstOrDefault(
                    job => job.Id == jobCompleted.Job.Id
                );
                if (activeJob == null)
                {
                    Log.Warning("Received unexpected JobCompleted notification from '{0}' (JobId = {1}, CorrelationId = {2}).",
                        Sender,
                        jobCompleted.Job.Id,
                        jobCompleted.CorrelationId
                    );

                    Unhandled(jobCompleted);

                    return;
                }

                IActorRef jobProcess;
                if (!_jobProcesses.TryGetValue(activeJob.Id, out jobProcess))
                {
                    Log.Warning("Cannot find process for job {0} (CorrelationId = {1}).",
                        activeJob.Id,
                        jobCompleted.CorrelationId
                    );

                    Unhandled(jobCompleted);

                    return;
                }

                Log.Info("Cleaning up job {0}...", activeJob.Id);

                jobProcess.Tell(
                    new Process.Destroy(jobCompleted.CorrelationId)
                );
            });
            Receive<NotifyWhenAllJobsCompleted>(notifyWhenAllJobsCompleted =>
            {
                if (_activeJobs.Count == 0)
                {
                    Sender.Tell(
                        new AllJobsCompleted()
                    );
                }
                else
                    _notifyJobProcessCompletion.Enqueue(Sender);
            });
            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef == _jobStore)
                {
                    Log.Warning("Job store has terminated; the dispatcher cannot continue.");

                    Unhandled(terminated); // Results in DeathPactException.

                    return;
                }

                // TODO: Improve job / message lookup (stop relying on correlation Id!).

                int jobId = -1;
                foreach (var jobIdAndJobProcess in _jobProcesses)
                {
                    if (jobIdAndJobProcess.Value == terminated.ActorRef)
                        jobId = jobIdAndJobProcess.Key;
                }

                if (jobId == -1)
                {
                    Log.Warning("Received unexpected actor-termination message; cannot determine job associated with process '{0}'.",
                        terminated.ActorRef
                    );

                    Unhandled(terminated); // Results in DeathPactException.

                    return;
                }

                _jobProcesses.Remove(jobId);

                string jobCorrelationId = null;
                foreach (var correlationIdAndActiveJob in _activeJobs)
                {
                    if (correlationIdAndActiveJob.Value.Id == jobId)
                        jobCorrelationId = correlationIdAndActiveJob.Key;
                }
                if (jobCorrelationId != null)
                    _activeJobs.Remove(jobCorrelationId);

                if (_activeJobs.Count + _jobProcesses.Count == 0)
                    NotifyAllJobsCompleted();
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
        ///     Notify interested parties that all active jobs have been completed.
        /// </summary>
        void NotifyAllJobsCompleted()
        {
            while (_notifyJobProcessCompletion.Count > 0)
            {
                _notifyJobProcessCompletion.Dequeue().Tell(
                    new AllJobsCompleted()
                );
            }
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
                () => new Dispatcher(stateDirectory, jobStore, launcher)
            );
        }
    }
}
