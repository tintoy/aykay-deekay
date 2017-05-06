using Akka;
using Akka.Actor;
using Docker.DotNet;
using System;

namespace AKDK.Actors
{
    /// <summary>
    ///     Represents a connection to the Docker API.
    /// </summary>
    public class DockerConnection
        : ReceiveActor
    {
        /// <summary>
        ///     The underlying docker API client for the current connection.
        /// </summary>
        DockerClient _client;

        /// <summary>
        ///     Create a new <see cref="DockerConnection"/> actor.
        /// </summary>
        /// <param name="client">
        ///     The underlying docker API client for the current connection.
        /// </param>
        public DockerConnection(DockerClient client)
        {
            _client = client;
        }

        /// <summary>
        ///     Called when the actor is stopping.
        /// </summary>
        protected override void PostStop()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }

            base.PostStop();
        }

        public static Props Create(DockerClient client)
        {
            return Props.Create(
                () => new DockerConnection(client)
            );
        }
    }
}