using Akka.Actor;
using System;

namespace AKDK.Messages
{
    using Utilities;

    /// <summary>
    ///     Response indicating a successful connection to a Docker API.
    /// </summary>
    public class Connected
        : CorrelatedMessage
    {
        /// <summary>
        ///     Create a new <see cref="Connected"/> message.
        /// </summary>
        /// <param name="client">
        ///     The client actor for the connection.
        /// </param>
        /// <param name="endpointUri">
        ///     The end-point URI for the Docker API.
        /// </param>
        /// <param name="apiVersion">
        ///     The Docker API version.
        /// </param>
        /// <param name="correlationId">
        ///     The message correlation Id.
        /// </param>
        public Connected(IActorRef client, Uri endpointUri, Version apiVersion, string correlationId)
            : base(correlationId)
        {
            Client = client;
            EndpointUri = endpointUri;
            ApiVersion = apiVersion;
        }

        /// <summary>
        ///     The client actor for the connection.
        /// </summary>
        public IActorRef Client { get; }

        /// <summary>
        ///     The end-point URI for the Docker API.
        /// </summary>
        public Uri EndpointUri { get; }

        /// <summary>
        ///     The Docker API version.
        /// </summary>
        public Version ApiVersion { get; }

        /// <summary>
        ///     Is the connection to the local Docker API?
        /// </summary>
        public bool IsLocal => EndpointUri == LocalDocker.EndPointUri;
    }
}
