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
        ///     The well-known name for the Docker event bus actor.
        /// </summary>
        public static readonly string ActorName = "event-bus";

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
        ///     Is the <see cref="Client"/> used to monitor events still alive?
        /// </summary>
        bool _isClientAlive;

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

                    Context.Stop(Self); // TODO: Is this actually the behaviour we're after?
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
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            Context.Watch(_client);

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

            if (_isClientAlive)
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
