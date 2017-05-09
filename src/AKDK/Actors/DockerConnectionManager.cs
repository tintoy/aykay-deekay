using Akka;
using Akka.Actor;
using Docker.DotNet;
using System;
using System.Collections.Generic;

namespace AKDK.Actors
{
    /// <summary>
    ///     Actor that manages <see cref="Connection"/> actors.
    /// </summary>
    public class DockerConnectionManager
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
        ///     Connection actors, keyed by API end-point URI.
        /// </summary>
        readonly Dictionary<Uri, IActorRef>                 _connectionActors = new Dictionary<Uri, IActorRef>();

        /// <summary>
        ///     Connection API end-point URIs, keyed by connector actor.
        /// </summary>
        readonly Dictionary<IActorRef, Uri>                 _connectionEndPoints = new Dictionary<IActorRef, Uri>();

        /// <summary>
        ///     Create a new <see cref="DockerConnectionManager"/> actor.
        /// </summary>
        public DockerConnectionManager()
        {
            Receive<Terminated>(terminated =>
            {
                Uri endPointUri;
                if (!_connectionEndPoints.TryGetValue(terminated.ActorRef, out endPointUri))
                {
                    // Not one of ours; this will result in DeathPactException.
                    Unhandled(terminated);

                    return;
                }

                Log.Info("Handling termination of actor '{0}' for end-point '{1}'.", terminated.ActorRef, endPointUri);

                _connectionActors.Remove(endPointUri);
                _connectionEndPoints.Remove(terminated.ActorRef);
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


        IActorRef CreateConnection(Uri endpointUri)
        {
            DockerClientConfiguration clientConfiguration = GetClientConfiguration(endpointUri);

            IActorRef connectionActor = Context.ActorOf(Connection.Create(
                clientConfiguration.CreateClient()
            ));
            _connectionActors.Add(endpointUri, connectionActor);
            _connectionEndPoints.Add(connectionActor, endpointUri);

            Context.Watch(connectionActor);

            return connectionActor;
        }

        DockerClientConfiguration GetClientConfiguration(Uri endpointUri)
        {
            DockerClientConfiguration clientConfiguration;
            if (!_configuration.TryGetValue(endpointUri, out clientConfiguration))
            {
                clientConfiguration = new DockerClientConfiguration(endpointUri);
                _configuration.Add(endpointUri, clientConfiguration);
            }

            return clientConfiguration;
        }
    }
}