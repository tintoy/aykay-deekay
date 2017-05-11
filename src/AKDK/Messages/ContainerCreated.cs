using Docker.DotNet.Models;
using System;
using System.Collections.Generic;

namespace AKDK.Messages
{
    /// <summary>
    ///		Response to <see cref="CreateContainer"/> indicating successful creation of a container via the Docker API.
    /// </summary>
    public class ContainerCreated
        : Response
    {
        /// <summary>
        ///     No warnings.
        /// </summary>
        static IReadOnlyList<string> NoWarnings = new string[0];

        /// <summary>
        ///		Create a new <see cref="ContainerCreated"/>.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that was assigned to the original <see cref="CreateContainer"/> request.
        /// </param>
        /// <param name="apiResponse">
        ///		The response from the Docker API.
        /// </param>
        public ContainerCreated(string correlationId, CreateContainerResponse apiResponse)
            : base(correlationId)
        {
            if (apiResponse == null)
                throw new ArgumentNullException(nameof(apiResponse));

            ApiResponse = apiResponse;
        }

        /// <summary>
        ///     The Id of the newly-created container.
        /// </summary>
        public string ContainerId => ApiResponse.ID;

        /// <summary>
        ///     Warnings (if any) associated with creation of the container.
        /// </summary>
        public IReadOnlyList<string> Warnings => ApiResponse.Warnings as IReadOnlyList<string> ?? NoWarnings;

        /// <summary>
        ///		The response from the Docker API.
        /// </summary>
        public CreateContainerResponse ApiResponse { get; }
    }
}
