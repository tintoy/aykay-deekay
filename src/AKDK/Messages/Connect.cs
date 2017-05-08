using AKDK.Utilities;
using Docker.DotNet;
using System;

namespace AKDK.Messages
{
    /// <summary>
    ///     Request to create a connection to a Docker API.
    /// </summary>
    public class Connect
        : Request
    {
        /// <summary>
        ///     Create a new <see cref="Connect"/> message.
        /// </summary>
        /// <param name="endpointUri">
        ///     The end-point URI for the Docker API.
        /// </param>
        /// <param name="credentials">
        ///     Optional credentials for authenticating to the Docker API.
        /// </param>
        /// <param name="correlationId">
        ///     An optional message correlation Id.
        /// </param>
        public Connect(Uri endpointUri, Credentials credentials = null, string correlationId = null)
            : base(correlationId)
        {
            EndpointUri = endpointUri;
            Credentials = credentials;
        }

        /// <summary>
        ///     The end-point URI for the Docker API.
        /// </summary>
        public Uri EndpointUri;

        /// <summary>
        ///     Optional credentials for authenticating to the Docker API.
        /// </summary>
        public Credentials Credentials;

        /// <summary>
        ///     Create a request to connect to the local Docker API.
        /// </summary>
        /// <param name="correlationId">
        ///     An optional message correlation Id.
        /// </param>
        /// <returns>
        ///     The <see cref="Connect"/> request.
        /// </returns>
        public static Connect Local(string correlationId) => new Connect(LocalDocker.EndPointUri);
    }
}
