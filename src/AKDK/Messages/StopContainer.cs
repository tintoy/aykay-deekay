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
        /// <param name="waitBeforeKillSeconds">
        ///     Optional <see cref="ContainerStopParameters"/> that control operation behaviour.
        /// </param>
        /// <param name="correlationId">
        ///		An optional message correlation Id (if not specified, a random value will be assigned to the request).
        /// </param>
        public StopContainer(string containerId, uint? waitBeforeKillSeconds = null, string correlationId = null)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            ContainerId = containerId;
            WaitBeforeKillSeconds = waitBeforeKillSeconds;
        }

        /// <summary>
        ///		The name or Id of the container to stop.
        /// </summary>
        public string ContainerId { get; }

        /// <summary>
        ///     An optional number of seconds to wait for the container to stop before killing it.
        /// </summary>
        public uint? WaitBeforeKillSeconds { get; }

        /// <summary>
        ///     A short name for the operation represented by the request.
        /// </summary>
        public override string OperationName => $"Stop Container ({ContainerId})";

    }
}
