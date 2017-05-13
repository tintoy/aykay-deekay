using Akka.Actor;
using System;
using System.Collections.Immutable;

namespace AKDK.Actors
{
    using System.Collections.Generic;
    using Messages;
    using Messages.DockerEvents;

    /// <summary>
    ///     Actor that manages the event bus for <see cref="DockerEvent"/>s.
    /// </summary>
    public class DockerEventBus
        : EventBusActor<DockerEvent>
    {
        /// <summary>
        ///     The well-known name for the Docker event bus actor.
        /// </summary>
        public static readonly string ActorName = "event-bus";

        /// <summary>
        ///     All Docker event types.
        /// </summary>
        static readonly ImmutableList<Type> AllDockerEventTypes = ImmutableList.CreateRange(
            new Type[] { typeof(ImageEvent), typeof(ContainerEvent), typeof(NetworkEvent) }
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
        ///     Is the <see cref="Client"/> used to monitor events still alive?
        /// </summary>
        bool _isClientAlive;

        /// <summary>
        ///     Are we monitoring events yet?
        /// </summary>
        bool _isMonitoringEvents;

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
            _isClientAlive = true;

            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef == client)
                {
                    _isClientAlive = false;
                    _isMonitoringEvents = false;

                    Context.Stop(Self); // TODO: Is this actually the behaviour we're after? Clients should not crash, but can't we request a new one?
                }
                else
                    Unhandled(terminated); // Results in DeathPactException
            });
        }

        /// <summary>
        ///     All event types known to the event bus.
        /// </summary>
        protected override ImmutableList<Type> AllEventTypes => AllDockerEventTypes;

        /// <summary>
        ///     Called when an actor has been subscribed to the specified event types.
        /// </summary>
        /// <param name="subscriber">
        ///     The actor that was subscribed.
        /// </param>
        /// <param name="eventTypes">
        ///     The types of event messages to which the actor was subscribed.
        /// </param>
        protected override void OnAddedSubscriber(IActorRef subscriber, IEnumerable<Type> eventTypes)
        {
            base.OnAddedSubscriber(subscriber, eventTypes);

            if (_isMonitoringEvents)
                return;

            // Start receiving events.
            _client.Tell(
                new MonitorContainerEvents(correlationId: _monitorEventsCorrelationId)
            );
            _isMonitoringEvents = true;
        }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            Context.Watch(_client);
        }

        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PostStop()
        {
            base.PostStop();

            if (_isClientAlive && _isMonitoringEvents)
            {
                // Stop receiving events.
                _client.Tell(
                    new CancelRequest(correlationId: _monitorEventsCorrelationId)
                );
            }
        }

        /// <summary>
        ///     Generate <see cref="Props"/> to create a new <see cref="DockerEventBus"/> actor.
        /// </summary>
        /// <param name="client">
        ///     A reference to the <see cref="Client"/> actor used to monitor events.
        /// </param>
        /// <returns>
        ///     The configured <see cref="Props"/>.
        /// </returns>
        public static Props Create(IActorRef client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return Props.Create(
                () => new DockerEventBus(client)
            );
        }
    }
}
