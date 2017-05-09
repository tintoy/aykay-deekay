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
                try
                {
                    Log.Debug("Received connection request '{0}' from '{1}' for '{2}'.",
                        connect.CorrelationId,
                        Sender.Path,
                        connect.EndpointUri
                    );

                    IActorRef client = GetOrCreateClient(connect);

                    Sender.Tell(new Connected(client,
                        endpointUri: connect.EndpointUri,
                        correlationId: connect.CorrelationId
                    ));
                }
                catch (Exception createClientError)
                {
                    Sender.Tell(new ConnectionFailure(
                        exception: createClientError,
                        correlationId: connect.CorrelationId
                    ));

                    throw; // Let supervision handle it.
                }                
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
        ///     Get the supervisor strategy for child actors.
        /// </summary>
        /// <returns>
        ///     The supervisor strategy.
        /// </returns>
        protected override SupervisorStrategy SupervisorStrategy()
        {
            // TODO: Determine correct strategy configuration.

            return new OneForOneStrategy(
                maxNrOfRetries: 5,
                withinTimeRange: TimeSpan.FromSeconds(5),
                decider: Decider.From(exception =>
                {
                    // TODO: Work out what types of exception should be handling here (and what the desired behaviour is).

                    switch (exception)
                    {
                        case DockerApiException dockerApiError:
                        {
                            return Directive.Resume;
                        }
                        default:
                        {
                            return Directive.Restart;
                        }
                    }
                })
            );
        }

        /// <summary>
        ///     Get or create the new Docker API <see cref="Client"/> actor for the specified <see cref="Connect"/> request.
        /// </summary>
        /// <param name="connectRequest">
        ///     The <see cref="Connect"/> request message.
        /// </param>
        /// <returns>
        ///     A reference to the <see cref="Client"/> actor.
        /// </returns>
        IActorRef GetOrCreateClient(Connect connectRequest)
        {
            if (connectRequest == null)
                throw new ArgumentNullException(nameof(connectRequest));
            
            DockerClientConfiguration clientConfiguration = GetClientConfiguration(
                connectRequest.EndpointUri,
                connectRequest.Credentials
            );

            IActorRef clientActor;
            if (!_clientActors.TryGetValue(connectRequest.EndpointUri, out clientActor))
            {
                clientActor = Context.ActorOf(
                    Props.Create<Client>(Connection.Create(
                        clientConfiguration.CreateClient() // TODO: Add constructor overload to inject configuration instead of client; let Client create the DockerClient (except in tests).
                    )),
                    name: $"client-{_nextClientId++}"
                );
                _clientActors.Add(connectRequest.EndpointUri, clientActor);
                _clientEndPoints.Add(clientActor, connectRequest.EndpointUri);

                Log.Debug("Created client '{0}' for connection request for '{1}' from '{2}' (CorrelationId = '{3}').",
                    clientActor.Path,
                    Sender.Path,
                    connectRequest.EndpointUri,
                    connectRequest.CorrelationId
                );
            }
            else
            {
                Log.Debug("Retrieved existing client '{0}' for connection request for '{1}' from '{2}' (CorrelationId = '{3}').",
                    clientActor.Path,
                    Sender.Path,
                    connectRequest.EndpointUri,
                    connectRequest.CorrelationId
                );
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