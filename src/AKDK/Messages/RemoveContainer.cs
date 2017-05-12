using System;
using Docker.DotNet.Models;

namespace AKDK.Messages
{
    /// <summary>
    ///     Request to remove a container.
    /// </summary>
    public class RemoveContainer
        : Request
    {
        /// <summary>
        ///     Create a new <see cref="RemoveContainer"/> message.
        /// </summary>
        /// <param name="containerId">
        ///     The name or Id of the container to remove.
        /// </param>
        /// <param name="parameters">
        ///     Optional <see cref="ContainerRemoveParameters"/> to control operation behaviour.
        /// </param>
        /// <param name="correlationId">
        ///     An optional message correlation Id.
        /// </param>
        public RemoveContainer(string containerId, string correlationId = null, ContainerRemoveParameters parameters = null)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            ContainerId = containerId;
            Parameters = parameters ?? new ContainerRemoveParameters();
        }

        /// <summary>
        ///     The name or Id of the container to delete.
        /// </summary>
        public string ContainerId { get; }

        /// <summary>
        ///     <see cref="ContainerRemoveParameters"/> to control operation behaviour.
        /// </summary>
        public ContainerRemoveParameters Parameters { get; }

        /// <summary>
        /// 
        /// A short name for the operation represented by the request.
        /// </summary>
        public override string OperationName => $"Delete container '{ContainerId}'";
    }
}
