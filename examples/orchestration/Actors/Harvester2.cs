using Akka.Actor;
using AKDK.Actors;
using System;
using System.IO;

namespace AKDK.Examples.Orchestration.Actors
{
    using Utilities;

    /// <summary>
    ///     Actor that collects output from completed jobs.
    /// </summary>
    public partial class Harvester2
        : ReceiveActorEx
    {
        public Harvester2()
        {
            Receive<Harvest>(harvest =>
            {
                Log.Info("Harvesting output for container '{0}' from '{1}'...",
                    harvest.ContainerId,
                    harvest.StateDirectory.FullName
                );

                FileInfo contentFile = harvest.StateDirectory.GetFile("content.txt");
                if (!contentFile.Exists)
                {
                    Log.Warning("Cannot find content file '{0}' in state directory for container '{1}'.",
                        contentFile.FullName,
                        harvest.ContainerId
                    );

                    Sender.Tell(new HarvestFailed(
                        harvest.CorrelationId,
                        harvest.ContainerId,
                        new InvalidOperationException($"Cannot find content file '{contentFile.FullName}' in state directory for container '{harvest.ContainerId}'.")
                    ));

                    return;
                }

                string content = contentFile.ReadAllText();
                
                Log.Debug("Cleaning up state directory '{0}' for container '{1}'...",
                    harvest.StateDirectory.FullName,
                    harvest.ContainerId
                );
                harvest.StateDirectory.Delete(recursive: true);

                Log.Debug("Cleaned up state directory for container '{0}'.", harvest.ContainerId);

                Sender.Tell(new Harvested(
                    harvest.CorrelationId,
                    harvest.ContainerId,
                    content
                ));
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
