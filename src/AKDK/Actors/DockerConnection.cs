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
        : ReceiveActorEx
    {
        /// <summary>
        ///     The underlying docker API client for the current connection.
        /// </summary>
        DockerClient _client;

        /// <summary>
        ///     Create a new <see cref="DockerConnection"/> actor.
        /// </summary>
        /// <param name="client">
        ///     The underlying docker API client.
        /// </param>
        public DockerConnection(DockerClient client)
        {
            _client = client;

            ReceiveSingleton<Close>(() =>
            {
                Log.Info("DockerConnection '{0}' Received stop request from '{1}' - will terminate.",
                    Self.Path.Name, Sender
                );

                Context.Stop(Self);
            });
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

        /// <summary>
        ///     Build <see cref="Props"/> to create a <see cref="DockerConnection"/> actor.
        /// </summary>
        /// <param name="client">
        ///     The underlying docker API client for the current connection.
        /// </param>
        /// <returns>
        ///     The configured <see cref="Props"/>.
        /// </returns>
        public static Props Create(DockerClient client)
        {
            return Props.Create(
                () => new DockerConnection(client)
            );
        }

        /// <summary>
        ///     Request to a <see cref="DockerConnection"/> requesting close of the underlying connection to the Docker API.
        /// </summary>
        public class Close
        {
            public static readonly Close Instance = new Close();

            Close() { }
        }
    }
}