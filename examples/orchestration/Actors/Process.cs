using Akka.Actor;
using AKDK.Actors;
using AKDK.Messages.DockerEvents;
using System;

namespace AKDK.Examples.Orchestration.Actors
{
    using Messages;

    // TODO: Implement Launcher actor that creates containers and then creates Process actors to manage them.

    /// <summary>
    ///     Actor that manages an instance of a Docker container.
    /// </summary>
    public class Process
        : ReceiveActorEx
    {
        /// <summary>
        ///     A reference to the <see cref="Client"/> actor for the Docker API.
        /// </summary>
        readonly IActorRef _client;

        /// <summary>
        ///     The name or Id of the container managed by the <see cref="Process"/> actor.
        /// </summary>
        readonly string _containerId;

        /// <summary>
        ///     Create a new <see cref="Process"/> actor.
        /// </summary>
        /// <param name="client">
        ///     A reference to the <see cref="Client"/> actor for the Docker API.
        /// </param>
        /// <param name="containerId">
        ///     The name or Id of the container managed by the <see cref="Process"/> actor.
        /// </param>
        public Process(IActorRef client, string containerId)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(containerId)}.", nameof(containerId));

            _client = client;
            _containerId = containerId;

            Receive<ContainerDestroyed>(containerDestroyed =>
            {
                if (!IsMyContainer(containerDestroyed))
                    return;

                Log.Debug("Container '{0}' has been destroyed; management actor '{1}' will shut down.", _containerId, Self);

                Context.Stop(Self);
            });
            Receive<ContainerEvent>(containerEvent =>
            {
                if (!IsMyContainer(containerEvent))
                    return;
                
                // TODO: Handle generic container event, if appropriate.
            });
        }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            _client.Tell(
                new EventBusActor.Subscribe(Self, eventTypes: new Type[]
                {
                    typeof(ContainerEvent)
                })
            );
        }

        /// <summary>
        ///     Determine whether an event refers to the container managed by the <see cref="Process"/>.
        /// </summary>
        /// <param name="containerEvent">
        ///     The <see cref="ContainerEvent"/>.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the event relates to the <see cref="Process"/>'s container; otherwise, <c>false</c>.
        /// </returns>
        bool IsMyContainer(ContainerEvent containerEvent)
        {
            if (containerEvent == null)
                throw new ArgumentNullException(nameof(containerEvent));

            // TODO: Implement AKAK.Actors.DockerContainerEventBus (attached to DockerEventBus, using container Id as a classifier).

            return containerEvent.ContainerId == _containerId;
        }
    }
}
