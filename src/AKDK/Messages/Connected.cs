using Akka.Actor;
using System;

namespace AKDK.Messages
{
    /// <summary>
    ///     Response indicating a successful connection to a Docker API.
    /// </summary>
    public class Connected
        : Response
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
        /// <param name="correlationId">
        ///     The message correlation Id.
        /// </param>
        public Connected(IActorRef client, Uri endpointUri, string correlationId)
            : base(correlationId)
        {
            Client = client;
            EndpointUri = endpointUri;
        }

        /// <summary>
        ///     The client actor for the connection.
        /// </summary>
        public IActorRef Client { get; }

        /// <summary>
        ///     The end-point URI for the Docker API.
        /// </summary>
        public Uri EndpointUri { get; }
    }
}
