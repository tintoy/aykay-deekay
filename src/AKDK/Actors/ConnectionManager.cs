using Akka.Actor;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Version = System.Version;

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
        ///     The minimum supported version of the Docker API.
        /// </summary>
        public static readonly Version MinimumDockerApiVersion = new Version("1.24");

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
            ReceiveAsync<Connect>(async connect =>
            {
                try
                {
                    Log.Debug("Received connection request '{0}' from '{1}' for '{2}'.",
                        connect.CorrelationId,
                        Sender.Path,
                        connect.EndpointUri
                    );

                    IActorRef client = await GetOrCreateClientAsync(connect);

                    Sender.Tell(new Connected(client,
                        endpointUri: connect.EndpointUri,
                        correlationId: connect.CorrelationId
                    ));
                }
                catch (Exception createClientError)
                {
                    Sender.Tell(new ConnectFailed(
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
        async Task<IActorRef> GetOrCreateClientAsync(Connect connectRequest)
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
                IDockerClient dockerClient = clientConfiguration.CreateClient();

                VersionResponse versionInfo;
                try
                {
                    versionInfo = await dockerClient.Miscellaneous.GetVersionAsync();
                }
                catch (TimeoutException connectionTimedOut)
                {
                    // TODO: More specific exception type.

                    throw new Exception($"Failed to connect to the Docker API at '{connectRequest.EndpointUri}' (connection timed out).",
                        innerException: connectionTimedOut
                    );
                }

                Version apiVersion = new Version(versionInfo.APIVersion);

                if (apiVersion < MinimumDockerApiVersion)
                    throw new NotSupportedException($"The Docker API at '{connectRequest.EndpointUri}' is v{apiVersion}, but AK/DK only supports v{MinimumDockerApiVersion} or newer.");

                Log.Debug("Successfully connected to Docker API (v{0}) at '{1}'.", apiVersion, connectRequest.EndpointUri);

                clientActor = Context.ActorOf(
                    Props.Create<Client>(
                        Connection.Create(dockerClient) // TODO: Add constructor overload to inject configuration instead of client; let Client create the DockerClient (except in tests).
                    ),
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