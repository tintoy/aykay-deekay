using Akka.Actor;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace AKDK.Actors
{
    using Messages;

    // TODO: Work out how to deal with response for GetContainerLogs - StreamLine message should somehow be transformed into a more suitable message (e.g. ContainerLogLine).
    // TODO: Maybe add a StreamLineTransform delegate to the ExecuteCommand message?
    // TODO: If so, there should be a base class for messages representing a client-level view of streamed response data.

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

                var executeCommand = new Connection.ExecuteCommand(listImages, async (dockerClient, cancellationToken) =>
                {
                    IList<ImagesListResponse> images = await dockerClient.Images.ListImagesAsync(listImages.Parameters);

                    return new ImageList(listImages.CorrelationId, images);
                });

                _connection.Tell(executeCommand, Sender);
            });
            Receive<GetContainerLogs>(getContainerLogs =>
            {
                Log.Debug("Received GetContainerLogs request '{0}' from '{1}'.", getContainerLogs.CorrelationId, Sender);

                var executeCommand = new Connection.ExecuteCommand(getContainerLogs, async (dockerClient, cancellationToken) =>
                {
                    Stream responseStream = await dockerClient.Containers.GetContainerLogsAsync(
                        getContainerLogs.ContainerId,
                        getContainerLogs.Parameters,
                        cancellationToken
                    );

                    return new StreamedResponse(getContainerLogs.CorrelationId, responseStream);
                });

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
