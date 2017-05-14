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
        : ReceiveActorEx
    {
        /// <summary>
        ///     Active jobs, keyed by message correlation Id.
        /// </summary>
        readonly Dictionary<string, Job>    _activeJobs = new Dictionary<string, Job>();

        /// <summary>
        ///     <see cref="Process"/> actors for active jobs, keyed by job Id.
        /// </summary>
        readonly Dictionary<int, IActorRef> _jobProcesses = new Dictionary<int, IActorRef>();

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

                _launcher.Tell(new Launcher.CreateProcess(
                    owner: Self,
                    imageName: "fetcher",
                    environmentVariables: new Dictionary<string, string>
                    {
                        ["TARGET_URL"] = jobCreated.Job.TargetUrl.AbsoluteUri
                    },
                    volumeMounts: new Dictionary<string, string>
                    {
                        // TODO: Create and mount state directory.
                    },
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

                Log.Info("Starting container '{0}' for job '{1}'...", processCreated.ContainerId, activeJob.Id);

                processCreated.Process.Tell(
                    new Process.Start(processCreated.CorrelationId)
                );
                _jobProcesses.Add(activeJob.Id, processCreated.Process);
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
                    status: processExited.ExitCode == 0 ? JobStatus.Completed : JobStatus.Failed,
                    appendMessages: new string[]
                    {
                        $"Job completed with exit code {processExited.ExitCode}."
                    }
                ));
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
