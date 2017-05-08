using Docker.DotNet;
using System;

namespace AKDK.Messages
{
    using Utilities;

    /// <summary>
    ///     Request to create a connection to a Docker API.
    /// </summary>
    public class Connect
        : Request
    {
        /// <summary>
        ///     The default TCP port for the docker API.
        /// </summary>
        public const int DefaultPort = 2375;

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
        public Connect(string endpointUri, Credentials credentials = null, string correlationId = null)
            : this(new Uri(endpointUri), credentials, correlationId)
        {
        }

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
        public Uri EndpointUri { get; }

        /// <summary>
        ///     Optional credentials for authenticating to the Docker API.
        /// </summary>
        public Credentials Credentials { get; }

        /// <summary>
        ///     Create a request to connect to the local Docker API.
        /// </summary>
        /// <param name="correlationId">
        ///     An optional message correlation Id.
        /// </param>
        /// <returns>
        ///     The <see cref="Connect"/> request.
        /// </returns>
        public static Connect Local(string correlationId = null) => new Connect(LocalDocker.EndPointUri);

        /// <summary>
        ///     Create a request to connect to a Docker API over TCP.
        /// </summary>
        /// <param name="hostName">
        ///     The target docker host name.
        /// </param>
        /// <param name="port">
        ///     The target TCP port.
        /// </param>
        /// <param name="correlationId">
        ///     An optional message correlation Id.
        /// </param>
        /// <returns>
        ///     The <see cref="Connect"/> request.
        /// </returns>
        public static Connect Tcp(string hostName, int port = DefaultPort, string correlationId = null) => new Connect(new Uri($"tcp://{hostName}:{port}"));
    }
}
