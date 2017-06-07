using Akka.Actor;
using AKDK.Actors;
using AKDK.Messages.DockerEvents;
using System;

namespace AKDK.Examples.Orchestration.Actors
{
    using Messages;

    /// <summary>
    ///     Actor that manages an instance of a Docker container.
    /// </summary>
    public partial class Process
    {
        public class Start
            : CorrelatedMessage
        {
            public Start(string correlationId = null)
                : base(correlationId)
            {
            }
        }

        public class Started
            : CorrelatedMessage
        {
            public Started(string correlationId, string containerId)
                : base(correlationId)
            {
                ContainerId = containerId;
            }

            public string ContainerId { get; }
        }

        public class Exited
            : CorrelatedMessage
        {
            public Exited(string correlationId, string containerId, int exitCode)
                : base(correlationId)
            {
                ContainerId = containerId;
                ExitCode = exitCode;
            }

            public string ContainerId { get; }
            public int ExitCode { get; }
        }

        public class Destroy
            : CorrelatedMessage
        {
            public Destroy(string correlationId)
                : base(correlationId)
            {
            }
        }

        public class Destroyed
            : CorrelatedMessage
        {
            public Destroyed(string correlationId, string containerId)
                : base(correlationId)
            {
                if (String.IsNullOrWhiteSpace(containerId))
                    throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

                ContainerId = containerId;
            }

            public string ContainerId { get; }
        }

        public class Stop
            : CorrelatedMessage
        {
            public Stop(string correlationId = null)
                : base(correlationId)
            {
            }
        }

        public class Stopped
            : CorrelatedMessage
        {
            public Stopped(string correlationId, string containerId)
                : base(correlationId)
            {
                ContainerId = containerId;
            }

            public string ContainerId { get; }
        }
    }
}
