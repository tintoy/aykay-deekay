using System;
using System.Collections.Generic;

namespace AKDK.Messages
{
    /// <summary>
    ///		Response to <see cref="RemoveContainer"/> indicating successful removal of a container via the Docker API.
    /// </summary>
    public class ContainerRemoved
        : Response
    {
        /// <summary>
        ///     No warnings.
        /// </summary>
        static IReadOnlyList<string> NoWarnings = new string[0];

        /// <summary>
        ///		Create a new <see cref="ContainerRemoved"/>.
        /// </summary>
        /// <param name="correlationId">
        ///		The message correlation Id that was assigned to the original <see cref="CreateContainer"/> request.
        /// </param>
        /// <param name="containerId">
        ///     The Id of the removed container.
        /// </param>
        public ContainerRemoved(string correlationId, string containerId)
            : base(correlationId)
        {
            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            ContainerId = containerId;
        }

        /// <summary>
        ///     The Id of the removed container.
        /// </summary>
        public string ContainerId { get; }
    }
}
