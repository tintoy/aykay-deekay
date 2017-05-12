using Akka.Actor;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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
        ///     A reference to the <see cref="DockerEventBus"/> actor used for pub/sub of streamed Docker events.
        /// </summary>
        IActorRef       _dockerEventBus;

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
                var executeCommand = new Connection.ExecuteCommand(listImages, async (dockerClient, cancellationToken) =>
                {
                    IList<ImagesListResponse> images = await dockerClient.Images.ListImagesAsync(listImages.Parameters);

                    return new ImageList(listImages.CorrelationId, images);
                });

                _connection.Tell(executeCommand, Sender);
            });
            Receive<CreateContainer>(createContainer =>
            {
                var executeCommand = new Connection.ExecuteCommand(createContainer, async (dockerClient, cancellationToken) =>
                {
                    CreateContainerResponse response = await dockerClient.Containers.CreateContainerAsync(createContainer.Parameters);

                    return new ContainerCreated(createContainer.CorrelationId, response);
                });

                _connection.Tell(executeCommand, Sender);
            });
            Receive<StartContainer>(startContainer =>
            {
                var executeCommand = new Connection.ExecuteCommand(startContainer, async (dockerClient, cancellationToken) =>
                {
                    bool containerWasStarted = await dockerClient.Containers.StartContainerAsync(startContainer.ContainerId, startContainer.Parameters);

                    return new ContainerStarted(startContainer.CorrelationId, startContainer.ContainerId,
                        alreadyStarted: !containerWasStarted
                    );
                });

                _connection.Tell(executeCommand, Sender);
            });
            Receive<RemoveContainer>(removeContainer =>
            {
                var executeCommand = new Connection.ExecuteCommand(removeContainer, async (dockerClient, cancellationToken) =>
                {
                    await dockerClient.Containers.RemoveContainerAsync(removeContainer.ContainerId, removeContainer.Parameters);

                    return new ContainerRemoved(removeContainer.CorrelationId,
                        removeContainer.ContainerId
                    );
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

                    return new StreamedResponse(getContainerLogs.CorrelationId, responseStream, format: StreamedResponseFormat.Log);
                });

                _connection.Tell(executeCommand, Sender);
            });
            Receive<MonitorContainerEvents>(monitorContainerEvents =>
            {
                Log.Debug("Received MonitorContainerEvents request '{0}' from '{1}'.", monitorContainerEvents.CorrelationId, Sender);

                var executeCommand = new Connection.ExecuteCommand(monitorContainerEvents, async (dockerClient, cancellationToken) =>
                {
                    Stream responseStream = await dockerClient.Miscellaneous.MonitorEventsAsync(monitorContainerEvents.Parameters, cancellationToken);

                    return new StreamedResponse(monitorContainerEvents.CorrelationId, responseStream, format: StreamedResponseFormat.Events);
                });

                _connection.Tell(executeCommand, Sender);
            });
            Receive<CancelRequest>(cancelRequest =>
            {
                _connection.Forward(cancelRequest);
            });
            Receive<EventBusActor.Subscribe>(subscribeToDockerEvents =>
            {
                if (_dockerEventBus == null)
                {
                    _dockerEventBus = Context.ActorOf(
                        DockerEventBus.Create(Self),
                        name: DockerEventBus.ActorName
                    );
                }

                _dockerEventBus.Forward(subscribeToDockerEvents);
            });
            Receive<EventBusActor.Unsubscribe>(unsubscribeFromDockerEvents =>
            {
                if (_dockerEventBus == null)
                    return;

                _dockerEventBus.Forward(unsubscribeFromDockerEvents);
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
