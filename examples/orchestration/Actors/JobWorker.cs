using Akka.Actor;
using AKDK.Actors;
using System;
using System.Collections.Generic;
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
        ///     A reference to the <see cref="Launcher"/> actor used to create the job's associated process.
        /// </summary>
        readonly IActorRef  _launcher;

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
        ///     Create a new <see cref="JobWorker"/>.
        /// </summary>
        /// <param name="launcher">
        ///     A reference to the <see cref="Launcher"/> actor used to create the job's associated process.
        /// </param>
        /// <param name="harvesterProps">
        ///     <see cref="Props"/> used to create the <see cref="Harvester"/> that collects the job output.
        /// </param>
        public JobWorker(IActorRef launcher)
        {
            if (launcher == null)
                throw new ArgumentNullException(nameof(launcher));
            
            _launcher = launcher;
            Context.Watch(launcher); // If launcher crashes, so do we.
            
            Become(Ready);
        }

        /// <summary>
        ///     Called when the actor is ready to handle requests.
        /// </summary>
        void Ready()
        {
            _requestor = null;
            _requestCorrelationId = null;
            _job = null;
            _jobStateDirectory = null;
            _process = null;

            Receive<ExecuteJob>(executeJob =>
            {
                _requestor = Sender;
                _requestCorrelationId = executeJob.CorrelationId;
                _job = executeJob.Job.WithStatus(JobStatus.Pending);
                _launcher.Tell(new Launcher.CreateProcess(
                    owner: Self,
                    imageName: "fetcher",
                    environmentVariables: new Dictionary<string, string>
                    {
                        ["TARGET_URL"] = executeJob.TargetUrl.AbsoluteUri
                    },
                    volumeMounts: new Dictionary<string, string>
                    {
                        [executeJob.JobStateDirectory.FullName] = "/root/state"
                    },
                    correlationId: _requestCorrelationId
                ));

                Become(Launching);
            });
        }

        void Launching()
        {
            // TODO: Implement launch timeout.

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
                _job = _job.WithStatus(JobStatus.Active);
                _requestor.Tell(
                    new JobExecuting(_job, _jobStateDirectory, processStarted.CorrelationId)
                );

                Become(Running);
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

        void Running()
        {
            Receive<Process.Exited>(processExited =>
            {
                JobStatus jobStatus = processExited.ExitCode == 0 ? JobStatus.Succeeded : JobStatus.Failed;
                _job = _job.WithStatus(jobStatus, $"Job completed with exit code {processExited.ExitCode}.");

                _requestor.Tell(
                    new JobExecuted(_job, _jobStateDirectory, processExited.CorrelationId)
                );

                Context.Unwatch(_process);

                Become(Ready);
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
    }
}
