using Akka.Actor;
using Akka.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Reflection;

namespace AKDK.Actors
{
    /// <summary>
    ///     Well-known messages for <see cref="EventBusActor{TEvent}"/>.
    /// </summary>
    public static class EventBusActor
    {
        public class Subscribe
        {
            public Subscribe(IActorRef subscriber, IEnumerable<Type> eventTypes = null, string correlationId = null)
            {
                Subscriber = subscriber;
                EventTypes = eventTypes != null ? ImmutableList.CreateRange(eventTypes) : ImmutableList<Type>.Empty;
                CorrelationId = correlationId;
            }

            public IActorRef Subscriber { get; }
            public ImmutableList<Type> EventTypes { get; }
            public string CorrelationId { get; }
        }

        public class Subscribed
        {
            public Subscribed(string correlationId, IEnumerable<Type> eventTypes)
            {
                CorrelationId = correlationId;
                EventTypes = eventTypes != null ? ImmutableList.CreateRange(eventTypes) : ImmutableList<Type>.Empty;
            }

            public string CorrelationId { get; }
            public ImmutableList<Type> EventTypes { get; }
        }

        public class Unsubscribe
        {
            public Unsubscribe(IActorRef subscriber, IEnumerable<Type> eventTypes = null, string correlationId = null)
            {
                Subscriber = subscriber;
                EventTypes = eventTypes != null ? ImmutableList.CreateRange(eventTypes) : ImmutableList<Type>.Empty;
                CorrelationId = correlationId;
            }

            public IActorRef Subscriber { get; }
            public ImmutableList<Type> EventTypes { get; }
            public string CorrelationId { get; }
        }

        public class Unsubscribed
        {
            public Unsubscribed(string correlationId, IEnumerable<Type> eventTypes)
            {
                CorrelationId = correlationId;
                EventTypes = eventTypes != null ? ImmutableList.CreateRange(eventTypes) : ImmutableList<Type>.Empty;
            }

            public string CorrelationId { get; }
            public ImmutableList<Type> EventTypes { get; }
        }
    }

    /// <summary>
    ///     The base class for actors that manage an event bus.
    /// </summary>
    public abstract class EventBusActor<TEvent>
        : ReceiveActorEx
        where TEvent : class
    {
        /// <summary>
        ///     Create a new <see cref="EventBusActor{TEvent}"/>.
        /// </summary>
        protected EventBusActor()
        {
            Receive<TEvent>(evt =>
            {
                PublishEvent(evt);
            });

            Receive<EventBusActor.Subscribe>(subscribe =>
            {
                ImmutableList<Type> eventTypes = subscribe.EventTypes;
                if (subscribe.EventTypes.IsEmpty)
                    eventTypes = AllEventTypes;

                AddSubscriber(subscribe.Subscriber, eventTypes);

                Sender.Tell(
                    new EventBusActor.Subscribed(subscribe.CorrelationId, eventTypes)
                );
            });
            Receive<EventBusActor.Unsubscribe>(unsubscribe =>
            {
                if (!unsubscribe.EventTypes.IsEmpty)
                    RemoveSubscriber(unsubscribe.Subscriber, unsubscribe.EventTypes);
                else
                    RemoveSubscriber(unsubscribe.Subscriber);

                Sender.Tell(
                    new EventBusActor.Unsubscribed(unsubscribe.CorrelationId, unsubscribe.EventTypes)
                );
            });
        }
        
        /// <summary>
        ///     The underlying event bus.
        /// </summary>
        EventBus Bus { get; } = new EventBus();

        /// <summary>
        ///     All known event types supported by the bus.
        /// </summary>
        protected abstract ImmutableList<Type> AllEventTypes { get; }

        /// <summary>
        ///     Publish an event to the underlying event bus.
        /// </summary>
        /// <param name="evt">
        ///     The event to publish.
        /// </param>
        protected void PublishEvent(TEvent evt)
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            Bus.Publish(evt);
        }

        /// <summary>
        ///     Subscribe an actor to the specified event types.
        /// </summary>
        /// <param name="subscriber">
        ///     The actor to subscribe.
        /// </param>
        /// <param name="eventTypes">
        ///     The types of event messages to which the actor will be subscribed.
        /// </param>
        protected void AddSubscriber(IActorRef subscriber, IEnumerable<Type> eventTypes)
        {
            foreach (Type eventType in eventTypes)
                Bus.Subscribe(subscriber, eventType);
        }

        /// <summary>
        ///     Unsubscribe an actor from the specified event types.
        /// </summary>
        /// <param name="subscriber">
        ///     The actor to unsubscribe.
        /// </param>
        /// <param name="eventTypes">
        ///     The types of event messages from which the actor will be unsubscribed.
        /// </param>
        protected void RemoveSubscriber(IActorRef subscriber, IEnumerable<Type> messageTypes)
        {
            foreach (Type messageType in messageTypes)
                Bus.Unsubscribe(subscriber, messageType);
        }

        /// <summary>
        ///     Unsubscribe an actor from all events.
        /// </summary>
        /// <param name="subscriber">
        ///     The actor to unsubscribe.
        /// </param>
        protected void RemoveSubscriber(IActorRef subscriber)
        {
            Bus.Unsubscribe(subscriber);
        }

        /// <summary>
        ///     An actor-oriented event bus that uses an event message's CLR type as the event classifier.
        /// </summary>
        protected class EventBus
            : ActorEventBus<TEvent, Type>
        {
            /// <summary>
            ///     Create a new <see cref="EventBus"/>.
            /// </summary>
            public EventBus()
            {
            }

            /// <summary>
            ///     Get a classifier for the specified event message.
            /// </summary>
            /// <param name="evt">
            ///     The event message.
            /// </param>
            /// <returns>
            ///     The message's CLR type.
            /// </returns>
            protected override Type GetClassifier(TEvent evt) => evt.GetType();

            /// <summary>
            ///     Determine whether the specified event message exactly matches the specified classifier.
            /// </summary>
            /// <param name="evt">
            ///     The event message.
            /// </param>
            /// <param name="eventType">
            ///     The event classifier.
            /// </param>
            /// <returns>
            ///     <c>true</c>, if the message is of the specified type; otherwise, <c>false</c>.
            /// </returns>
            protected override bool Classify(TEvent evt, Type eventType) => eventType.IsInstanceOfType(evt);

            /// <summary>
            ///     Determine whether the specified event message matches the specified classifier or a sub-classifier.
            /// </summary>
            /// <param name="evt">
            ///     The event message.
            /// </param>
            /// <param name="eventType">
            ///     The event classifier.
            /// </param>
            /// <returns>
            ///     <c>true</c>, if the message is of the specified type or a sub-type; otherwise, <c>false</c>.
            /// </returns>
            protected override bool IsSubClassification(Type parent, Type child) => parent.IsAssignableFrom(child);

            /// <summary>
            ///     Publish an event to a subscriber.
            /// </summary>
            /// <param name="evt">
            ///     The event message to publish.
            /// </param>
            /// <param name="subscriber">
            ///     The actor to which the message will be published.
            /// </param>
            protected override void Publish(TEvent evt, IActorRef subscriber) => subscriber.Tell(evt);
        }
    }
}