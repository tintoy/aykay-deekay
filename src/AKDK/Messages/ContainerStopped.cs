using Docker.DotNet.Models;
using System;
using System.Collections.Generic;

namespace AKDK.Messages
{
    /// <summary>
    ///		Response to <see cref="StopContainer"/> indicating successful stop of a container via the Docker API.
    /// </summary>
    public class ContainerStopped
        : Response
    {
        /// <summary>
        ///		Create a new <see cref="ContainerStopped"/>.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that was assigned to the original <see cref="CreateContainer"/> request.
        /// </param>
        /// <param name="containerId">
        ///     The Id of the target container.
        /// </param>
        /// <param name="alreadyStopped">
        ///		Was the container already stopped?
        /// </param>
        public ContainerStopped(string correlationId, string containerId, bool alreadyStopped)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            ContainerId = containerId;
            AlreadyStopped = alreadyStopped;
        }

        /// <summary>
        ///     The Id of the target container.
        /// </summary>
        public string ContainerId { get; }

        /// <summary>
        ///		Was the container already stopped?
        /// </summary>
        public bool AlreadyStopped { get; }
    }
}
