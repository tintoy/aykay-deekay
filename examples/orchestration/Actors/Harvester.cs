using Akka.Actor;
using AKDK.Actors;
using AKDK.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor that collects output from completed jobs.
    /// </summary>
    public class Harvester
        : ReceiveActorEx
    {
        /// <summary>
        ///     The directory that contains job-specific state directories.
        /// </summary>
        readonly DirectoryInfo _stateDirectory;

        /// <summary>
        ///     A reference to the <see cref="JobStore"/> actor.
        /// </summary>
        readonly IActorRef      _jobStore;

        public Harvester(DirectoryInfo stateDirectory, IActorRef jobStore)
        {
            if (stateDirectory == null)
                throw new ArgumentNullException(nameof(stateDirectory));

            if (jobStore == null)
                throw new ArgumentNullException(nameof(jobStore));

            _stateDirectory = stateDirectory;
            _jobStore = jobStore;

            Receive<JobStoreEvents.JobSucceeded>(jobSucceeded =>
            {
                DirectoryInfo jobStateDirectory = new DirectoryInfo(Path.Combine(
                    _stateDirectory.FullName, $"job-{jobSucceeded.Job.Id}"
                ));

                Log.Info("Harvesting output for job {0} from '{1}'...",
                    jobSucceeded.Job.Id,
                    jobStateDirectory.FullName
                );

                FileInfo contentFile = new FileInfo(Path.Combine(
                    jobStateDirectory.FullName, "content.txt"
                ));
                if (!contentFile.Exists)
                {
                    Log.Warning("Cannot find content file '{0}' for job {1}.",
                        contentFile.FullName,
                        jobSucceeded.Job.Id
                    );

                    _jobStore.Tell(new JobStore.UpdateJob(
                        jobId: jobSucceeded.Job.Id,
                        status: JobStatus.Failed,
                        appendMessages: new string[] { $"Cannot find content file '{contentFile.FullName}'." },
                        correlationId: jobSucceeded.CorrelationId
                    ));

                    return;
                }

                _jobStore.Tell(new JobStore.UpdateJob(
                    jobId: jobSucceeded.Job.Id,
                    status: JobStatus.Completed,
                    appendMessages: new string[] { $"Resulting content harvested from file '{contentFile.FullName}'." },
                    content: File.ReadAllText(contentFile.FullName),
                    correlationId: jobSucceeded.CorrelationId
                ));

                Log.Debug("Cleaning up state directory '{0}' for job {1}...",
                    jobStateDirectory.FullName,
                    jobSucceeded.Job.Id
                );
                jobStateDirectory.Delete(recursive: true);

                Log.Debug("Cleaned up state directory for job {0}.", jobSucceeded.Job.Id);
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            Context.Watch(_jobStore);

            _jobStore.Tell(new EventBusActor.Subscribe(Self, eventTypes: new Type[]
            {
                typeof(JobStoreEvents.JobSucceeded)
            }));
        }
    }
}
