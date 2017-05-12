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
        ///     The well-known name for the top-level Docker connection management actor.
        /// </summary>
        public static readonly string ActorName = "connection-manager";

        /// <summary>
        ///     The minimum supported version of the Docker API.
        /// </summary>
        public static readonly Version MinimumDockerApiVersion = new Version("1.24");

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

                    IActorRef client = await CreateClientAsync(connect);

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
        }

        /// <summary>
        ///     Get the supervisor strategy for child actors.
        /// </summary>
        /// <returns>
        ///     The supervisor strategy.
        /// </returns>
        protected override SupervisorStrategy SupervisorStrategy()
        {
            // TODO: Determine correct strategy for supervising HTTP clients.

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
                        case TimeoutException timeoutError:
                        {
                            // AF: What actually *is* the correct behaviour when dealing with a timeout?
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
        ///     Create a new Docker API <see cref="Client"/> actor for the specified <see cref="Connect"/> request.
        /// </summary>
        /// <param name="connectRequest">
        ///     The <see cref="Connect"/> request message.
        /// </param>
        /// <returns>
        ///     A reference to the <see cref="Client"/> actor.
        /// </returns>
        async Task<IActorRef> CreateClientAsync(Connect connectRequest)
        {
            if (connectRequest == null)
                throw new ArgumentNullException(nameof(connectRequest));
            
            DockerClientConfiguration clientConfiguration = new DockerClientConfiguration(
                endpoint: connectRequest.EndpointUri,
                credentials: connectRequest.Credentials
            );

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

            IActorRef clientActor = Context.ActorOf(
                Props.Create<Client>(
                    Connection.Create(dockerClient) // TODO: Add constructor overload to inject configuration instead of client; let Client create the DockerClient (except in tests).
                ),
                name: $"client-{_nextClientId++}"
            );

            Log.Debug("Created client '{0}' for connection request for '{1}' from '{2}' (CorrelationId = '{3}').",
                clientActor.Path,
                Sender.Path,
                connectRequest.EndpointUri,
                connectRequest.CorrelationId
            );

            return clientActor;
        }

        /// <summary>
        ///     Generate <see cref="Props"/> to create a new <see cref="ConnectionManager"/>.
        /// </summary>
        /// <returns>
        ///     The configured <see cref="Props"/>.
        /// </returns>
        public static Props Create() => Props.Create(
            () => new ConnectionManager()
        );
    }
}
