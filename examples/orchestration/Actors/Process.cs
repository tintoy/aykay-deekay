using Akka.Actor;
using AKDK.Actors;
using AKDK.Messages;
using Docker.DotNet.Models;
using System;

using DockerEvents = AKDK.Messages.DockerEvents;

namespace AKDK.Examples.Orchestration.Actors
{
    /// <summary>
    ///     Actor that manages an instance of a Docker container.
    /// </summary>
    public partial class Process
        : ReceiveActorEx, IWithUnboundedStash
    {
        /// <summary>
        ///     The actor that owns the <see cref="Process"/>.
        /// </summary>
        readonly IActorRef  _owner;

        /// <summary>
        ///     A reference to the <see cref="Client"/> actor for the Docker API.
        /// </summary>
        readonly IActorRef  _client;

        /// <summary>
        ///     The name or Id of the container managed by the <see cref="Process"/> actor.
        /// </summary>
        readonly string     _containerId;

        /// <summary>
        ///     The message correlation Id used for notifications from the <see cref="Process"/>.
        /// </summary>
        /// <remarks>
        ///     Captured when the <see cref="Start"/> message is received.
        /// </remarks>
        string     _correlationId;

        /// <summary>
        ///     Create a new <see cref="Process"/> actor.
        /// </summary>
        /// <param name="owner">
        ///     The actor that owns the <see cref="Process"/>.
        /// </param>
        /// <param name="client">
        ///     A reference to the <see cref="Client"/> actor for the Docker API.
        /// </param>
        /// <param name="containerId">
        ///     The name or Id of the container managed by the <see cref="Process"/> actor.
        /// </param>
        public Process(IActorRef owner, IActorRef client, string containerId)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            _owner = owner;
            _client = client;
            _containerId = containerId;
        }

        /// <summary>
        ///     The actor's message stash.
        /// </summary>
        public IStash Stash { get; set; }

        void Initializing()
        {
            Log.Debug("Process '{0}' initialising...", _containerId);

            Receive<EventBusActor.Subscribed>(subscribed =>
            {
                Become(Created);
            });
            Receive<Start>(start =>
            {
                Stash.Stash();
            });
            Receive<Stop>(stop =>
            {
                Stash.Stash();
            });
            Receive<Destroy>(destroy =>
            {
                Stash.Stash();
            });
            ReceiveContainerEvent<DockerEvents.ContainerEvent>(containerEvent =>
            {
                Stash.Stash();
            });
        }

        /// <summary>
        ///     Called when the process is first created.
        /// </summary>
        void Created()
        {
            Log.Debug("Process '{0}' initialised.", _containerId);

            Stash.UnstashAll();

            Receive<Start>(start =>
            {
                _correlationId = start.CorrelationId;
                _client.Tell(new StartContainer(_containerId,
                    correlationId: start.CorrelationId
                ));

                Become(Starting);
            });
            Receive<Stop>(stop =>
            {
                Sender.Tell(new OperationFailure(stop.CorrelationId,
                    operationName: "Kill Process",
                    reason: new InvalidOperationException("Process is not running.")
                ));
            });
            ReceiveContainerEvent<DockerEvents.ContainerDestroyed>(containerDestroyed =>
            {
                Log.Debug("Container '{0}' has been destroyed unexpectedly; management actor '{1}' will shut down.", _containerId, Self);

                Context.Stop(Self);
            });
        }

        /// <summary>
        ///     Called when the container is starting.
        /// </summary>
        void Starting()
        {
            Log.Debug("Process '{0}' starting...", _containerId);

            Receive<Start>(start =>
            {
                Sender.Tell(new OperationFailure(start.CorrelationId,
                    operationName: "Start Process",
                    reason: new InvalidOperationException("Process is already running.")
                ));
            });
            Receive<Destroy>(destroy =>
            {
                _client.Tell(new RemoveContainer(_containerId,
                    correlationId: destroy.CorrelationId
                ));

                Become(Destroying);
            });
            Receive<Stop>(stop =>
            {
                Log.Debug("Stopping container '{0}' for process '{1}'.",
                    _containerId,
                    Self
                );

                _client.Tell(new StopContainer(_containerId,
                    waitBeforeKillSeconds: 30,
                    correlationId: stop.CorrelationId
                ));

                Become(Stopping);
            });
            Receive<ContainerStarted>(containerStarted =>
            {
                Log.Debug("Container '{0}' started for process '{1}'.",
                    containerStarted.ContainerId,
                    Self
                );

                _owner.Tell(new Started(_correlationId,
                    containerId: containerStarted.ContainerId
                ));

                Become(Running);
            });
            Receive<ErrorResponse>(errorResponse =>
            {
                Log.Error(errorResponse.Reason, "Failed to start container '{0}': {1}",
                    _containerId,
                    errorResponse.Reason.Message
                );

                _owner.Forward(errorResponse);

                Context.Stop(Self);
            });
            ReceiveContainerEvent<DockerEvents.ContainerDied>(containerDied =>
            {
                Log.Debug("Container '{0}' has terminated unexpectedly with exit code {1}.", _containerId, containerDied.ExitCode);

                _owner.Tell(new Exited(_correlationId,
                    containerId: containerDied.ContainerId,
                    exitCode: containerDied.ExitCode
                ));

                Context.Stop(Self);
            });
            ReceiveContainerEvent<DockerEvents.ContainerDestroyed>(containerDestroyed =>
            {
                Log.Debug("Container '{0}' has been destroyed unexpectedly; management actor '{1}' will shut down.", _containerId, Self);

                Context.Stop(Self);
            });
        }

        /// <summary>
        ///     Called when the process is running.
        /// </summary>
        void Running()
        {
            Log.Debug("Process '{0}' running...", _containerId);

            Receive<Start>(start =>
            {
                Sender.Tell(new OperationFailure(start.CorrelationId,
                    operationName: "Start Process",
                    reason: new InvalidOperationException("Process is already running.")
                ));
            });
            Receive<Stop>(stop =>
            {
                Log.Debug("Stopping container '{0}' for process '{1}'.",
                    _containerId,
                    Self
                );

                _client.Tell(new StopContainer(_containerId,
                    correlationId: stop.CorrelationId
                ));

                Become(Stopping);
            });
            Receive<Destroy>(destroy =>
            {
                Sender.Tell(new OperationFailure(destroy.CorrelationId,
                    operationName: "Destroy Process",
                    reason: new InvalidOperationException("Process is still running.")
                ));
            });
            ReceiveContainerEvent<DockerEvents.ContainerDied>(containerDied =>
            {
                Log.Debug("Container '{0}' has terminated with exit code {1}.", _containerId, containerDied.ExitCode);

                _owner.Tell(new Exited(_correlationId,
                    containerId: containerDied.ContainerId,
                    exitCode: containerDied.ExitCode
                ));

                Become(Completed);
            });
            ReceiveContainerEvent<DockerEvents.ContainerDestroyed>(containerDestroyed =>
            {
                Log.Debug("Container '{0}' has been destroyed unexpectedly; management actor '{1}' will shut down.", _containerId, Self);

                Context.Stop(Self);
            });
        }

        /// <summary>
        ///     Called when the process is stopping.
        /// </summary>
        void Stopping()
        {
            Receive<ContainerStopped>(containerStopped =>
            {
                Log.Debug("Container '{0}' stopped for process '{1}'.",
                    containerStopped.ContainerId,
                    Self
                );

                _owner.Tell(new Stopped(_correlationId,
                    containerId: containerStopped.ContainerId
                ));
            });
            Receive<ErrorResponse>(errorResponse =>
            {
                Log.Error(errorResponse.Reason, "Failed to stop container '{0}': {1}",
                    _containerId,
                    errorResponse.Reason.Message
                );

                _owner.Forward(errorResponse);

                Context.Stop(Self);
            });
            ReceiveContainerEvent<DockerEvents.ContainerDied>(containerDied =>
            {
                Log.Debug("Container '{0}' has terminated with exit code {1}.", _containerId, containerDied.ExitCode);

                _owner.Tell(new Exited(_correlationId,
                    containerId: containerDied.ContainerId,
                    exitCode: containerDied.ExitCode
                ));

                Become(Completed);
            });

            Receive<Start>(start =>
            {
                Sender.Tell(new OperationFailure(start.CorrelationId,
                    operationName: "Start Process",
                    reason: new InvalidOperationException("Process is still stopping.")
                ));
            });
            Receive<Destroy>(destroy =>
            {
                Sender.Tell(new OperationFailure(destroy.CorrelationId,
                    operationName: "Destroy Process",
                    reason: new InvalidOperationException("Process is still stopping.")
                ));
            });
        }

        /// <summary>
        ///     Called when the process has exited.
        /// </summary>
        void Completed()
        {
            Receive<Start>(start =>
            {
                Sender.Tell(new OperationFailure(start.CorrelationId,
                    operationName: "Start Process",
                    reason: new InvalidOperationException("Process is already running.")
                ));
            });
            Receive<Destroy>(destroy =>
            {
                _client.Tell(new RemoveContainer(_containerId,
                    correlationId: destroy.CorrelationId
                ));

                Become(Destroying);
            });
        }

        /// <summary>
        ///     Called when the process is being destroyed.
        /// </summary>
        void Destroying()
        {
            Log.Debug("Container '{0}' is being destroyed...", _containerId);

            Receive<ContainerRemoved>(containerRemoved =>
            {
                Log.Debug("Container '{0}' has been destroyed; management actor '{1}' will shut down.", _containerId, Self);

                _owner.Tell(new Destroyed(containerRemoved.CorrelationId,
                    containerId: _containerId
                ));

                Context.Stop(Self);
            });
            Receive<Start>(start =>
            {
                Sender.Tell(new OperationFailure(start.CorrelationId,
                    operationName: "Start Process",
                    reason: new InvalidOperationException("Process is already running.")
                ));
            });
            Receive<Destroy>(destroy =>
            {
                Sender.Tell(new OperationFailure(destroy.CorrelationId,
                    operationName: "Destroy Process",
                    reason: new InvalidOperationException("Process is still running.")
                ));
            });
        }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            Context.Watch(_client);

            _client.Tell(
                new EventBusActor.Subscribe(Self, eventTypes: new Type[]
                {
                    typeof(DockerEvents.ContainerEvent)
                })
            );

            Context.Watch(_owner);

            Become(Created);
        }

        /// <summary>
        ///     Register a handler for the specified container event type.
        /// </summary>
        /// <typeparam name="TContainerEvent">
        ///     The type of container event to handle.
        /// </typeparam>
        /// <param name="handler">
        ///     The handler.
        /// </param>
        void ReceiveContainerEvent<TContainerEvent>(Action<TContainerEvent> handler)
            where TContainerEvent : DockerEvents.ContainerEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Receive<TContainerEvent>(containerEvent =>
            {
                // Only interested in events for our own container.
                if (containerEvent.ContainerId != _containerId)
                    return;

                handler(containerEvent);
            });
        }

        /// <summary>
        ///     Generate <see cref="Props"/> to create a new <see cref="Process"/> actor.
        /// </summary>
        /// <param name="owner">
        ///     The actor that owns the <see cref="Process"/>.
        /// </param>
        /// <param name="client">
        ///     A reference to the <see cref="Client"/> actor for the Docker API.
        /// </param>
        /// <param name="containerId">
        ///     The name or Id of the container managed by the <see cref="Process"/> actor.
        /// </param>
        /// <returns>
        ///     The configured <see cref="Props"/>.
        /// </returns>
        public static Props Create(IActorRef owner, IActorRef client, string containerId)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            return Props.Create(
                () => new Process(owner, client, containerId)
            );
        }
    }
}
