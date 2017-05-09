using Akka;
using Akka.Actor;
using Docker.DotNet;
using System;
using System.Collections.Generic;

namespace AKDK.Actors
{
    using Messages;

    /// <summary>
    ///     Actor that manages <see cref="Client"/> / <see cref="Connection"/> actors.
    /// </summary>
    public class ConnectionManager
        : ReceiveActorEx
    {
        /// <summary>
        ///     The Docker connection manager.
        /// </summary>
        public static readonly string ActorName = "docker-connection-manager";

        /// <summary>
        ///     Client configuration, keyed API end-point URI.
        /// </summary>
        readonly Dictionary<Uri, DockerClientConfiguration> _configuration = new Dictionary<Uri, DockerClientConfiguration>();

        /// <summary>
        ///     Client actors, keyed by API end-point URI.
        /// </summary>
        readonly Dictionary<Uri, IActorRef>                 _clientActors = new Dictionary<Uri, IActorRef>();

        /// <summary>
        ///     Connection API end-point URIs, keyed by Client actor.
        /// </summary>
        readonly Dictionary<IActorRef, Uri>                 _clientEndPoints = new Dictionary<IActorRef, Uri>();

        /// <summary>
        ///     The identifier for the next Client actor.
        /// </summary>
        int _nextClientId = 1;

        /// <summary>
        ///     Create a new <see cref="ConnectionManager"/> actor.
        /// </summary>
        public ConnectionManager()
        {
            Receive<Connect>(connect =>
            {
                IActorRef client = CreateClient(connect.EndpointUri, connect.Credentials);
                
                Sender.Tell(new Connected(client,
                    endpointUri: connect.EndpointUri,
                    correlationId: connect.CorrelationId
                ));
            });
            Receive<Terminated>(terminated =>
            {
                Uri endPointUri;
                if (!_clientEndPoints.TryGetValue(terminated.ActorRef, out endPointUri))
                {
                    // Not one of ours; this will result in DeathPactException.
                    Unhandled(terminated);

                    return;
                }

                Log.Info("Handling termination of client actor '{0}' for end-point '{1}'.", terminated.ActorRef, endPointUri);

                _clientActors.Remove(endPointUri);
                _clientEndPoints.Remove(terminated.ActorRef);
            });
        }

        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PostStop()
        {
            foreach (DockerClientConfiguration clientConfiguration in _configuration.Values)
                clientConfiguration.Dispose();

            _configuration.Clear();

            base.PostStop();
        }

        /// <summary>
        ///     Create a new Docker API <see cref="Client"/> actor.
        /// </summary>
        /// <param name="endpointUri">
        ///     The end-point URI for the Docker API.
        /// </param>
        /// <param name="credentials">
        ///     Optional credentials for authenticating to the Docker API.
        /// </param>
        /// <returns></returns>
        IActorRef CreateClient(Uri endpointUri, Credentials credentials)
        {
            if (endpointUri == null)
                throw new ArgumentNullException(nameof(endpointUri));

            DockerClientConfiguration clientConfiguration = GetClientConfiguration(endpointUri, credentials);

            IActorRef clientActor;
            if (!_clientActors.TryGetValue(endpointUri, out clientActor))
            {
                clientActor = Context.ActorOf(
                    Props.Create<Client>(Connection.Create(
                        clientConfiguration.CreateClient() // TODO: Add constructor overload to inject configuration instead of client; let Client create the DockerClient (except in tests).
                    )),
                    name: $"client-{_nextClientId++}"
                );
                _clientActors.Add(endpointUri, clientActor);
                _clientEndPoints.Add(clientActor, endpointUri);
            }

            Context.Watch(clientActor);

            return clientActor;
        }

        /// <summary>
        ///     Get or create the <see cref="DockerClientConfiguration"/> for the specified end-point URI and credentials.
        /// </summary>
        /// <param name="endpointUri">
        ///     The Docker end-point URI.
        /// </param>
        /// <param name="credentials">
        ///     Optional credentials for authenticating to the Docker API.
        /// </param>
        /// <returns>
        ///     The <see cref="DockerClientConfiguration"/>.
        /// </returns>
        DockerClientConfiguration GetClientConfiguration(Uri endpointUri, Credentials credentials)
        {
            if (endpointUri == null)
                throw new ArgumentNullException(nameof(endpointUri));

            DockerClientConfiguration clientConfiguration;
            if (!_configuration.TryGetValue(endpointUri, out clientConfiguration))
            {
                clientConfiguration = new DockerClientConfiguration(endpointUri, credentials);
                _configuration.Add(endpointUri, clientConfiguration);
            }

            return clientConfiguration;
        }
    }
}