using Docker.DotNet.Models;
using System;

namespace AKDK.Messages
{
    /// <summary>
    ///		Request stop of an existing container from the Docker API.
    /// </summary>
    public class StopContainer
        : Request
    {
        /// <summary>
        ///		Create a new <see cref="StopContainer"/> message.
        /// </summary>
        /// <param name="containerId">
        ///		The name or Id of the container to stop.
        /// </param>
        /// <param name="parameters">
        ///     Optional <see cref="ContainerStopParameters"/> that control operation behaviour.
        /// </param>
        /// <param name="correlationId">
        ///		An optional message correlation Id (if not specified, a random value will be assigned to the request).
        /// </param>
        public StopContainer(string containerId, ContainerStopParameters parameters = null, string correlationId = null)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            ContainerId = containerId;
            Parameters = parameters ?? new ContainerStopParameters();
        }

        /// <summary>
        ///		The name or Id of the container to stop.
        /// </summary>
        public string ContainerId { get; }

        /// <summary>
        ///     <see cref="ContainerStopParameters"/> that control operation behaviour.
        /// </summary>
        public ContainerStopParameters Parameters { get; }

        /// <summary>
        ///     A short name for the operation represented by the request.
        /// </summary>
        public override string OperationName => $"Stop Container ({ContainerId})";

    }
}
