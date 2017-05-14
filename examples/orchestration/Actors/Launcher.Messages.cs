using Akka.Actor;
using AKDK.Messages;
using System.Collections.Immutable;
using System.Collections.Generic;
using System;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor that launches and manages <see cref="Process"/>es.
    /// </summary>
    public partial class Launcher
    {
        /// <summary>
        ///     Request to create a new process.
        /// </summary>
        public class CreateProcess
            : CorrelatedMessage
        {
            /// <summary>
            ///     Create a new <see cref="CreateProcess"/> message.
            /// </summary>
            /// <param name="owner">
            ///     The actor that will own the resulting <see cref="Process"/> actor.
            /// </param>
            /// <param name="imageName">
            ///     Name (and, optionally, tag) of the image from which to create the process container.
            /// </param>
            /// <param name="environmentVariables">
            ///     Environment variables (if any) passed to the process container.
            /// </param>
            /// <param name="volumeMounts">
            ///     Volumes (if any) to mount in the process container.
            /// </param>
            /// <param name="entryPoint">
            ///     The command (if any) to act as the container entry-point.
            /// </param>
            /// <param name="correlationId">
            ///     An optional message correlation Id.
            /// </param>
            public CreateProcess(IActorRef owner, string imageName, IReadOnlyDictionary<string, string> environmentVariables = null, IReadOnlyDictionary<string, string> volumeMounts = null, string entryPoint = null, string correlationId = null)
                : base(correlationId)
            {
                if (owner == null)
                    throw new System.ArgumentNullException(nameof(owner));

                if (String.IsNullOrWhiteSpace(imageName))
                    throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(imageName)}.", nameof(imageName));

                Owner = owner;
                ImageName = imageName;
                EnvironmentVariables = environmentVariables != null ? ImmutableDictionary.CreateRange(environmentVariables) : ImmutableDictionary<string, string>.Empty;
                VolumeMounts = environmentVariables != null ? ImmutableDictionary.CreateRange(volumeMounts) : ImmutableDictionary<string, string>.Empty;
                EntryPoint = entryPoint;
            }

            /// <summary>
            ///     The actor that will own the resulting <see cref="Process"/> actor.
            /// </summary>
            public IActorRef Owner { get; }

            /// <summary>
            ///     Name (and, optionally, tag) of the image from which to create the process container.
            /// </summary>
            public string ImageName { get; }

            /// <summary>
            ///     Environment variables (if any) passed to the process container.
            /// </summary>
            /// <remarks>
            ///     Format is ["Name"] = "Value".
            /// </remarks
            public ImmutableDictionary<string, string> EnvironmentVariables { get; }

            /// <summary>
            ///     Volumes (if any) to mount in the process container.
            /// </summary>
            /// <remarks>
            ///     Format is ["HostPath"] = "ContainerPath".
            /// </remarks>
            public ImmutableDictionary<string, string> VolumeMounts { get; }

            /// <summary>
            ///     The command (if any) to act as the container entry-point.
            /// </summary>
            /// <remarks>
            ///     If not specified, then the image's default entry-point will be used.
            /// </remarks>
            public string EntryPoint { get; }
        }

        /// <summary>
        ///     Response indicating that a process has been created.
        /// </summary>
        public class ProcessCreated
            : CorrelatedMessage
        {
            /// <summary>
            ///     Create a new <see cref="ProcessCreated"/> message.
            /// </summary>
            /// <param name="correlationId">
            ///     The message correlation Id.
            /// </param>
            /// <param name="containerId">
            ///     The Id of the process container.
            /// </param>
            /// <param name="process">
            ///     A reference to the <see cref="Process"/> actor that manages the process container.
            /// </param>
            public ProcessCreated(string correlationId, string containerId, IActorRef process)
                : base(correlationId)
            {
                if (String.IsNullOrWhiteSpace(containerId))
                    throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

                if (process == null)
                    throw new ArgumentNullException(nameof(process));

                ContainerId = containerId;
                Process = process;
            }

            /// <summary>
            ///     The Id of the process container.
            /// </summary>
            public string ContainerId { get; }

            /// <summary>
            ///     A reference to the <see cref="Process"/> actor that manages the process container.
            /// </summary>
            public IActorRef Process { get; }
        }

        /// <summary>
        ///     Response indicating that a creation of a process has failed.
        /// </summary>
        public class ProcessCreateFailed
            : CorrelatedMessage
        {
            /// <summary>
            ///     Create a new <see cref="ProcessCreateFailed"/> message.
            /// </summary>
            /// <param name="correlationId">
            ///     The message correlation Id.
            /// </param>
            /// <param name="errorResponse">
            ///     An <see cref="ErrorResponse"/> indicating the reason for the failure.
            /// </param>
            public ProcessCreateFailed(string correlationId, ErrorResponse errorResponse)
                : base(correlationId)
            {
                ErrorResponse = errorResponse;
            }

            /// <summary>
            ///     An <see cref="Exception"/> indicating the reason for the failure.
            /// </summary>
            public Exception Reason => ErrorResponse.Reason;

            /// <summary>
            ///     An <see cref="ErrorResponse"/> indicating the reason for the failure.
            /// </summary>
            public ErrorResponse ErrorResponse { get; }
        }
    }
}
