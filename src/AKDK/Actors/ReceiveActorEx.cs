using Akka.Actor;
using Akka.Event;
using System;

namespace AKDK.Actors
{
    /// <summary>
    ///     The base class for actors that use Receive&lt;TMessage&gt;() and friends.
    /// </summary>
    public abstract class ReceiveActorEx
        : ReceiveActor
    {
        /// <summary>
		///		The logger for the actor (lazily-initialised).
		/// </summary>
		readonly Lazy<ILoggingAdapter> _log;

        /// <summary>
		///		Create a new <see cref="ReceiveActorEx"/>.
		/// </summary>
		protected ReceiveActorEx()
        {
            _log = new Lazy<ILoggingAdapter>(() =>
            {
                ILogMessageFormatter logMessageFormatter = CreateLogMessageFormatter() ?? new DefaultLogMessageFormatter();

                return Context.GetLogger(logMessageFormatter);
            });
        }

        /// <summary>
		///		The logger for the actor.
		/// </summary>
		protected ILoggingAdapter Log => _log.Value;

        /// <summary>
        ///     The system scheduler.
        /// </summary>
        protected IScheduler Scheduler => Context.System.Scheduler;

        /// <summary>
        ///     Register a handler for a singleton message.
        /// </summary>
        /// <param name="handler">
        ///     The handler.
        /// </param>
        protected virtual void ReceiveSingleton<TMessage>(Action handler)
        {
            if (handler == null)
                throw new ArgumentException(nameof(handler));

            Receive<TMessage>(_ =>
            {
                handler();
            });
        }

        /// <summary>
        ///     Register a handler for a singleton message.
        /// </summary>
        /// <param name="handler">
        ///     The handler (returns <c>true</c>, if the message was handled; otherwise, <c>false</c>).
        /// </param>
        protected virtual void ReceiveSingleton<TMessage>(Func<bool> handler)
        {
            if (handler == null)
                throw new ArgumentException(nameof(handler));

            Receive<TMessage>(_ =>
            {
                return handler();
            });
        }

        /// <summary>
		///		Create the log message formatter to be used by the actor.
		/// </summary>
		/// <returns>
		///		The log message formatter.
		/// </returns>
		protected virtual ILogMessageFormatter CreateLogMessageFormatter()
        {
            return new DefaultLogMessageFormatter();
        }

        /// <summary>
        ///     Schedule a message to be delivered repeatedly to the current actor.
        /// </summary>
        /// <param name="interval">
        ///     The delay between messages.
        /// </param>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="immediately">
        ///     Deliver the first message immediately?
        ///
        ///     If <c>false</c>, then the first message will be delivered after one <paramref name="interval"/> has elapsed.
        /// </param>
        protected void ScheduleTellSelfRepeatedly(TimeSpan interval, object message, bool immediately = false)
        {
            Scheduler.ScheduleTellRepeatedly(
                initialDelay: immediately ? TimeSpan.Zero : interval,
                interval: TimeSpan.FromSeconds(1),
                receiver: Self,
                message: message,
                sender: Self
             );
        }

        /// <summary>
        ///     Schedule a message to be delivered repeatedly to the current actor, with cancellation support.
        /// </summary>
        /// <param name="interval">
        ///     The delay between messages.
        /// </param>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="immediately">
        ///     Deliver the first message immediately?
        ///
        ///     If <c>false</c>, then the first message will be delivered after one <paramref name="interval"/> has elapsed.
        /// </param>
        /// <returns>
        ///     An <see cref="ICancelable"/> that can be used to cancel message delivery.
        /// </returns>
        protected ICancelable ScheduleTellSelfRepeatedlyCancelable(TimeSpan interval, object message, bool immediately = false)
        {
            return Scheduler.ScheduleTellRepeatedlyCancelable(
                initialDelay: immediately ? TimeSpan.Zero : interval,
                interval: interval,
                receiver: Self,
                message: message,
                sender: Self
             );
        }

        /// <summary>
        ///     Schedule a message to be delivered once to the current actor.
        /// </summary>
        /// <param name="delay">
        ///     The delay before the message is delivered.
        /// </param>
        /// <param name="message">
        ///     The message.
        /// </param>
        protected void ScheduleTellSelfOnce(TimeSpan delay, object message)
        {
            Scheduler.ScheduleTellOnce(
                delay: delay,
                receiver: Self,
                message: message,
                sender: Self
             );
        }

        /// <summary>
        ///     Schedule a message to be delivered once to the current actor, with cancellation support.
        /// </summary>
        /// <param name="delay">
        ///     The delay before the message is delivered.
        /// </param>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <returns>
        ///     An <see cref="ICancelable"/> that can be used to cancel message delivery.
        /// </returns>
        protected ICancelable ScheduleTellSelfOnceCancelable(TimeSpan delay, object message)
        {
            return Scheduler.ScheduleTellOnceCancelable(
                delay: delay,
                receiver: Self,
                message: message,
                sender: Self
             );
        }
    }
}