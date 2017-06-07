using System;

namespace AKDK.Messages
{
    /// <summary>
    ///		Request start of an existing container from the Docker API.
    /// </summary>
    public class StartContainer
        : Request
    {
        /// <summary>
        ///		Create a new <see cref="StartContainer"/> message.
        /// </summary>
        /// <param name="containerId">
        ///		The name or Id of the container to start.
        /// </param>
        /// <param name="detachKeys">
        ///     Optional key sequence used to detach from the container.
        /// </param>
        /// <param name="correlationId">
        ///		An optional message correlation Id (if not specified, a random value will be assigned to the request).
        /// </param>
        public StartContainer(string containerId, string detachKeys = null, string correlationId = null)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            ContainerId = containerId;
            DetachKeys = detachKeys;
        }

        /// <summary>
        ///		The name or Id of the container to start.
        /// </summary>
        public string ContainerId { get; }

        /// <summary>
        ///     Optional key sequence used to detach from the container.
        /// </summary>
        public string DetachKeys { get; }

        /// <summary>
        ///     A short name for the operation represented by the request.
        /// </summary>
        public override string OperationName => $"Start Container ({ContainerId})";

    }
}
