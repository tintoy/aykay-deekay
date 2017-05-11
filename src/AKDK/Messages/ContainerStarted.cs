using Docker.DotNet.Models;
using System;
using System.Collections.Generic;

namespace AKDK.Messages
{
    /// <summary>
    ///		Response to <see cref="StartContainer"/> indicating successful start of a container via the Docker API.
    /// </summary>
    public class ContainerStarted
        : Response
    {
        /// <summary>
        ///		Create a new <see cref="ContainerStarted"/>.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that was assigned to the original <see cref="CreateContainer"/> request.
        /// </param>
        /// <param name="containerId">
        ///     The Id of the target container.
        /// </param>
        /// <param name="alreadyStarted">
        ///		Was the container already running?
        /// </param>
        public ContainerStarted(string correlationId, string containerId, bool alreadyStarted)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            ContainerId = containerId;
            AlreadyStarted = alreadyStarted;
        }

        /// <summary>
        ///     The Id of the target container.
        /// </summary>
        public string ContainerId { get; }

        /// <summary>
        ///		Was the container already running?
        /// </summary>
        public bool AlreadyStarted { get; }
    }
}
