using Akka.Actor;
using AKDK.Actors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace AKDK.Examples.Orchestration.Actors
{
    // TODO: Decide whether to merge in functionality from Harvester, or simply trigger harvesting by sending a command message to a Harvester instance.

    /// <summary>
    ///     Actor that manages the execution of a specific job.
    /// </summary>
    /// <remarks>
    ///     TODO: Have a pool of these contact the dispatcher to announce availability rather than having the dispatcher create them directly for each job.
    /// </remarks>
    public partial class JobWorker
        : ReceiveActorEx
    {
        /// <summary>
        ///     The default timeout period when waiting for a job process.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30); // TODO: Make this configurable.

        /// <summary>
        ///     A reference to the <see cref="Launcher"/> actor used to create the job's associated process.
        /// </summary>
        readonly IActorRef  _launcher;

        /// <summary>
        ///     A reference to the <see cref="Harvester2"/> actor used to harvest content from completed job processes.
        /// </summary>
        readonly IActorRef  _harvester;

        /// <summary>
        ///     The actor that requested execution of the current job.
        /// </summary>
        IActorRef           _requestor;

        /// <summary>
        ///     The message correlation Id associated with the request for the current job.
        /// </summary>
        string              _requestCorrelationId;

        /// <summary>
        ///     The current job.
        /// </summary>
        Job                 _job;

        /// <summary>
        ///     The directory used to hold state for the current job.
        /// </summary>
        DirectoryInfo       _jobStateDirectory;

        /// <summary>
        ///     The process (if any) representing the current job.
        /// </summary>
        IActorRef           _process;

        /// <summary>
        ///     Cancellation for the pending timeout message (if any).
        /// </summary>
        ICancelable         _timeoutCancellation;

        /// <summary>
        ///     Create a new <see cref="JobWorker"/>.
        /// </summary>
        /// <param name="launcher">
        ///     A reference to the <see cref="Launcher"/> actor used to create the job's associated process.
        /// </param>
        /// <param name="harvester">
        ///     A reference to the <see cref="Harvester2"/> actor used to harvest content from completed job processes.
        /// </param>
        public JobWorker(IActorRef launcher, IActorRef harvester)
        {
            if (launcher == null)
                throw new ArgumentNullException(nameof(launcher));

            if (harvester == null)
                throw new ArgumentNullException(nameof(harvester));

            _launcher = launcher;
            Context.Watch(launcher); // If launcher crashes, so do we.

            _harvester = harvester;
            Context.Watch(_harvester); // If launcher crashes, so do we.

            Become(Ready);
        }

        /// <summary>
        ///     Called when the worker is ready to handle requests.
        /// </summary>
        void Ready()
        {
            _requestor = null;
            _requestCorrelationId = null;
            _job = null;
            _jobStateDirectory = null;
            _process = null;
            _timeoutCancellation = null;

            Receive<ExecuteJob>(executeJob =>
            {
                _requestor = Sender;
                _requestCorrelationId = executeJob.CorrelationId;
                _job = executeJob.Job.WithStatus(JobStatus.Pending);
                _jobStateDirectory = executeJob.JobStateDirectory;
                _launcher.Tell(new Launcher.CreateProcess(
                    owner: Self,
                    imageName: "fetcher",
                    environmentVariables: new Dictionary<string, string>
                    {
                        ["TARGET_URL"] = executeJob.TargetUrl.AbsoluteUri
                    }.ToImmutableDictionary(),
                    binds: new Dictionary<string, string>
                    {
                        [executeJob.JobStateDirectory.FullName] = "/root/state"
                    }.ToImmutableDictionary(),
                    correlationId: _requestCorrelationId
                ));

                Become(ProcessLaunching);
            });
        }

        /// <summary>
        ///     Called when the worker is launching the job process.
        /// </summary>
        void ProcessLaunching()
        {
            _timeoutCancellation = ScheduleTellSelfOnceCancelable(
                delay: DefaultTimeout,
                message: ProcessLaunchTimeout.Instance
            );

            Receive<Launcher.ProcessCreated>(processCreated =>
            {
                _process = processCreated.Process;
                Context.Watch(_process);

                _process.Tell(
                    new Process.Start(processCreated.CorrelationId)
                );
            });
            Receive<Process.Started>(processStarted =>
            {
                _timeoutCancellation.Cancel();
                _timeoutCancellation = null;

                _job = _job.WithStatus(JobStatus.Active);
                _requestor.Tell(
                    new JobExecuting(_job, _jobStateDirectory, processStarted.CorrelationId)
                );

                Become(ProcessRunning);
            });
            Receive<ProcessLaunchTimeout>(_ =>
            {
                Log.Error("Timed out waiting for process launch.");
            });
            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef.Equals(_process))
                {
                    _job = _job.WithStatus(JobStatus.Failed, "Job failed because process actor terminated unexpectedly.");
                    _requestor.Tell(
                        new JobExecuted(_job, _jobStateDirectory, _requestCorrelationId)
                    );

                    Become(Ready);
                }
            });
        }

        /// <summary>
        ///     Called when the job process is running.
        /// </summary>
        void ProcessRunning()
        {
            Receive<Process.Exited>(processExited =>
            {
                JobStatus jobStatus = processExited.ExitCode == 0 ? JobStatus.Succeeded : JobStatus.Failed;
                _job = _job.WithStatus(jobStatus, $"Job completed with exit code {processExited.ExitCode}.");
                _requestor.Tell(
                    new JobExecuting(_job, _jobStateDirectory, processExited.CorrelationId)
                );

                Context.Unwatch(_process);

                // TODO: Decide whether harvesting is appropriate when process exit code is non-zero.
                //       This would make much more sense if harvesting also included program output.

                _harvester.Tell(new Harvester2.Harvest(
                    containerId: processExited.ContainerId,
                    stateDirectory: _jobStateDirectory,
                    correlationId: _requestCorrelationId
                ));

                Become(ProcessHarvesting);
            });
            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef.Equals(_process))
                {
                    _job = _job.WithStatus(JobStatus.Failed, "Job failed because process actor terminated unexpectedly.");
                    _requestor.Tell(
                        new JobExecuted(_job, _jobStateDirectory, _requestCorrelationId)
                    );

                    Become(Ready);
                }
            });
        }

        /// <summary>
        ///     Called when content is being harvested from the current job's process container.
        /// </summary>
        void ProcessHarvesting()
        {
            _timeoutCancellation = ScheduleTellSelfOnceCancelable(
                delay: DefaultTimeout,
                message: ContainerHarvestTimeout.Instance
            );

            Receive<Harvester2.Harvested>(harvested =>
            {
                _job = _job.WithContent(harvested.Content).WithStatus(JobStatus.Completed);

                _requestor.Tell(
                    new JobExecuted(_job, _jobStateDirectory, _requestCorrelationId)
                );
            });
            Receive<Harvester2.HarvestFailed>(harvestFailed =>
            {
                Log.Error(harvestFailed.Reason, "Failed to harvest content for job {0}.", _job.Id);

                _job = _job.WithStatus(JobStatus.Failed,
                    "Unexpected error while harvesting content: " + harvestFailed.Reason.Message
                );

                _requestor.Tell(
                    new JobExecuted(_job, _jobStateDirectory, _requestCorrelationId)
                );
            });
            Receive<ContainerHarvestTimeout>(_ =>
            {
                Log.Error("Timed out waiting for container harvest.");

                _job = _job.WithStatus(JobStatus.Failed,
                    "Job timed out while harvesting content."
                );

                _requestor.Tell(
                    new JobExecuted(_job, _jobStateDirectory, _requestCorrelationId)
                );

                Become(Ready);
            });
        }
    }
}
