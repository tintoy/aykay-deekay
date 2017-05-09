using Docker.DotNet.Models;
using System;

namespace AKDK.Messages
{
    /// <summary>
    ///     Request to retrieve logs for a container.
    /// </summary>
    public class GetContainerLogs
        : Request
    {
        /// <summary>
        ///     Create a new <see cref="GetContainerLogs"/> message.
        /// </summary>
        /// <param name="containerId">
        ///     The Id of the target container.
        /// </param>
        /// <param name="parameters">
        ///     <see cref="ContainerLogsParameters"/> used to control operation behaviour.
        /// </param>
        /// <param name="correlationId">
        ///     An optional message correlation Id (ContainerLogEntry messages will be delivered with this correlation Id).
        /// </param>
        public GetContainerLogs(string containerId, ContainerLogsParameters parameters, string correlationId = null)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            ContainerId = containerId;
            Parameters = parameters;
        }

        /// <summary>
        ///     The Id of the target container.
        /// </summary>
        public string ContainerId { get; }

        /// <summary>
        ///     <see cref="ContainerLogsParameters"/> used to control operation behaviour.
        /// </summary>
        public ContainerLogsParameters Parameters { get; }

        /// <summary>
        ///     A short name for the operation represented by the request.
        /// </summary>
        public override string OperationName => "Get Container Logs";

    }
}
