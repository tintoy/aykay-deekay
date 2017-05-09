using Akka.Actor;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;

namespace AKDK.Actors
{
    using Messages;

    /// <summary>
    ///     Actor that aggregates a <see cref="Connection"/>, providing a public surface for the Docker API.
    /// </summary>
    public class Client
        : ReceiveActorEx
    {
        /// <summary>
        ///     <see cref="Props"/> that can be used to create the <see cref="Connection"/> actor used to execute <see cref="Connection.Command"/>s.
        /// </summary>
        readonly Props  _connectionProps;

        /// <summary>
        ///     A reference to the <see cref="Connection"/> actor used to execute <see cref="Connection.Command"/>s.
        /// </summary>
        IActorRef       _connection;

        /// <summary>
        ///     Create a new <see cref="Client"/> actor.
        /// </summary>
        /// <param name="connectionProps">
        ///     <see cref="Props"/> that can be used to create the <see cref="Connection"/> actor used to execute <see cref="Connection.Command"/>s.
        /// </param>
        public Client(Props connectionProps)
        {
            if (connectionProps == null)
                throw new ArgumentNullException(nameof(connectionProps));

            _connectionProps = connectionProps;
        }

        /// <summary>
        ///     Create a new <see cref="Client"/> actor.
        /// </summary>
        /// <param name="connection">
        ///     A reference to the <see cref="Connection"/> actor used to execute <see cref="Connection.Command"/>s.
        /// </param>
        /// <remarks>
        ///     Used for testing purposes (e.g. inject TestProbe).
        /// </remarks>
        public Client(IActorRef connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            _connection = connection;
        }

        /// <summary>
        ///     Called when the <see cref="Client"/> is ready to handle requests.
        /// </summary>
        void Ready()
        {
            // TODO: Handle termination of underlying Connection actor.

            Receive<ListImages>(listImages =>
            {
                Log.Debug("Received ListImages request '{0}' from '{1}'.", listImages.CorrelationId, Sender);

                // TODO: Out here, we know where to send the response.

                var executeCommand = new Connection.ExecuteCommand(listImages, async dockerClient =>
                {
                    // TODO: But in here, we don't.

                    IList<ImagesListResponse> images = await dockerClient.Images.ListImagesAsync(listImages.Parameters);

                    return new ImageList(listImages.CorrelationId, images);
                });

                // TODO: So, for now, we just forward the request (the reply gets sent to our sender).
                _connection.Tell(executeCommand, Sender);
            });
        }

        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            if (_connection == null)
                _connection = Context.ActorOf(_connectionProps, name: "connection");

            Context.Watch(_connection);

            Become(Ready);
        }
    }
}
