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
        ///     A reference to the <see cref="Connection"/> actor used to execute <see cref="Connection.Command"/>s.
        /// </summary>
        readonly IActorRef _connection;

        /// <summary>
        ///     Create a new <see cref="Client"/> actor.
        /// </summary>
        /// <param name="connection">
        ///     A reference to the <see cref="Connection"/> actor used to execute <see cref="Connection.Command"/>s.
        /// </param>
        public Client(IActorRef connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            _connection = connection;

            // TODO: Handle termination of underlying Connection actor.

            Receive<ListImages>(listImages =>
            {
                // TODO: Out here, we know where to send the response.

                var executeCommand = new Connection.ExecuteCommand(listImages, async dockerClient =>
                {
                    // TODO: But in here, we don't.

                    IList<ImagesListResponse> images = await dockerClient.Images.ListImagesAsync(listImages.Parameters);

                    return new ImageList(listImages.CorrelationId, images);
                });

                // TODO: So, for now, we just forward the request (the reply gets sent to our sender).
                _connection.Forward(executeCommand);
            });
        }

        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            Context.Watch(_connection);
        }
    }
}
