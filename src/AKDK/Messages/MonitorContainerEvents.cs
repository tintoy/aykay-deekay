using Docker.DotNet.Models;
using System;

namespace AKDK.Messages
{
    /// <summary>
    ///     Request to monitor container-related events.
    /// </summary>
    public class MonitorContainerEvents
        : Request
    {
        /// <summary>
        ///     Create a new <see cref="MonitorContainerEvents"/> message.
        /// </summary>
        /// <param name="parameters">
        ///     Optional <see cref="ContainerEventsParameters"/> used to control operation behaviour.
        /// </param>
        /// <param name="correlationId">
        ///     An optional message correlation Id.
        /// </param>
        public MonitorContainerEvents(ContainerEventsParameters parameters = null, string correlationId = null)
            : base(correlationId)
        {
            Parameters = parameters ?? new ContainerEventsParameters();
        }

        /// <summary>
        ///     <see cref="ContainerEventsParameters"/> used to control operation behaviour.
        /// </summary>
        public ContainerEventsParameters Parameters { get; }

        /// <summary>
        ///     A short name for the operation represented by the request.
        /// </summary>
        public override string OperationName => "Monitor Container Events";
    }
}
