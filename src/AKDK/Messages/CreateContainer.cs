using Docker.DotNet.Models;
using System;

namespace AKDK.Messages
{
    /// <summary>
    ///		Request creation of a new container from the Docker API.
    /// </summary>
    public class CreateContainer
        : Request
    {
        /// <summary>
        ///		Create a new <see cref="CreateContainer"/> message.
        /// </summary>
        /// <param name="parameters">
        ///		<see cref="CreateContainerParameters"/> used to control operation behaviour.
        /// </param>
        /// <param name="correlationId">
        ///		An optional message correlation Id (if not specified, a random value will be assigned to the request).
        /// </param>
        public CreateContainer(CreateContainerParameters parameters, string correlationId = null)
            : base(correlationId)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            Parameters = parameters;
        }

        /// <summary>
        ///		<see cref="CreateContainerParameters"/> used to control operation behaviour.
        /// </summary>
        public CreateContainerParameters Parameters { get; }

        /// <summary>
        ///     A short name for the operation represented by the request.
        /// </summary>
        public override string OperationName => $"Create Container ({Parameters.Image})";

    }
}
