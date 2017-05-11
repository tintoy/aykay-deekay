using Akka.Actor;
using System;
using System.Collections.Immutable;

namespace AKDK.Actors
{
    using Messages;
    using Messages.DockerEvents;

    /// <summary>
    ///     Actor that manages the event bus for <see cref="DockerEvent"/>s.
    /// </summary>
    public class DockerEventBus
        : EventBusActor<DockerEvent>
    {
        /// <summary>
        ///     All Docker event types.
        /// </summary>
        static readonly ImmutableList<Type> AllDockerEventTypes = ImmutableList.CreateRange(
            new Type[] { typeof(ImageEvent), typeof(ContainerEvent) }
        );

        /// <summary>
        ///     The correlation Id used to start and stop monitoring events.
        /// </summary>
        readonly string _monitorEventsCorrelationId = CorrelatedMessage.NewCorrelationId();

        /// <summary>
        ///     A reference to the <see cref="Client"/> actor.
        /// </summary>
        readonly IActorRef _client;

        /// <summary>
        ///     Create a new <see cref="DockerEventBus"/> actor.
        /// </summary>
        /// <param name="client">
        ///     A reference to the <see cref="Client"/> actor representing the connection for which events will be streamed.
        /// </param>
        public DockerEventBus(IActorRef client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            _client = client;
        }

        /// <summary>
        ///     All event types known to the event bus.
        /// </summary>
        protected override ImmutableList<Type> AllEventTypes => AllDockerEventTypes;

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            // Start receiving events.
            _client.Tell(
                new MonitorContainerEvents(correlationId: _monitorEventsCorrelationId)
            );
        }

        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PostStop()
        {
            base.PostStop();

            // Stop receiving events.
            _client.Tell(
                new CancelRequest(correlationId: _monitorEventsCorrelationId)
            );
        }
    }
}
