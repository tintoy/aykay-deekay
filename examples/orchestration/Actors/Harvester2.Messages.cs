using AKDK.Messages;
using System;
using System.IO;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor that collects output from completed jobs.
    /// </summary>
    public partial class Harvester2
    {
        public class Harvest
            : CorrelatedMessage
        {
            public Harvest(string containerId, DirectoryInfo stateDirectory, string correlationId = null)
                : base(correlationId)
            {
                if (String.IsNullOrWhiteSpace(containerId))
                    throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

                if (stateDirectory == null)
                    throw new ArgumentNullException(nameof(stateDirectory));

                ContainerId = containerId;
                StateDirectory = stateDirectory;
            }

            public string ContainerId { get; }

            public DirectoryInfo StateDirectory { get; }
        }

        public class Harvested
            : CorrelatedMessage
        {
            public Harvested(string correlationId, string containerId, string content)
                : base(correlationId)
            {
                if (String.IsNullOrWhiteSpace(containerId))
                    throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

                if (content == null)
                    throw new ArgumentNullException(nameof(content));

                ContainerId = containerId;
                Content = content;
            }

            public string ContainerId { get; }

            public string Content { get; }
        }

        public class HarvestFailed
            : CorrelatedMessage
        {
            public HarvestFailed(string correlationId, string containerId, Exception reason)
                : base(correlationId)
            {
                if (String.IsNullOrWhiteSpace(containerId))
                    throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

                if (reason == null)
                    throw new ArgumentNullException(nameof(reason));


                ContainerId = containerId;
                Reason = reason;
            }

            public string ContainerId { get; }

            public Exception Reason { get; }
        }
    }
}
