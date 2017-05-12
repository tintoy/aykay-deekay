using Akka.Actor;
using System;

namespace AKDK.Actors
{
    using Messages;

    /// <summary>
    ///     The root management actor for AK/DK.
    /// </summary>
    public class DockerManager
        : ReceiveActorEx
    {
        /// <summary>
        ///     The well-known name for the top-level Docker management actor.
        /// </summary>
        public static readonly string ActorName = "docker";

        /// <summary>
        ///     The top-level connection management actor.
        /// </summary>
        IActorRef _connectionManager;

        /// <summary>
        ///     Create a new <see cref="DockerManager"/> actor.
        /// </summary>
        public DockerManager()
        {
        }

        /// <summary>
        ///     Called when the actor is ready to handle requests.
        /// </summary>
        void Ready()
        {
            Receive<Connect>(connectionRequest =>
            {
                _connectionManager.Forward(connectionRequest);
            });
        }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            _connectionManager = Context.ActorOf(
                ConnectionManager.Create(),
                name: ConnectionManager.ActorName
            );
            Context.Watch(_connectionManager); // We don't handle the corresponding Terminate message, so their death will kill us.

            Become(Ready);
        }

        /// <summary>
        ///     Get the supervisor strategy for the Docker management actor's children.
        /// </summary>
        /// <returns>
        ///     The configured supervisor strategy.
        /// </returns>
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new AllForOneStrategy(
                maxNrOfRetries: 5,
                withinTimeRange: TimeSpan.FromSeconds(5),
                decider: Decider.From(exception =>
                {
                    // If any of our children encounter an unhandled error, we should restart all of them.
                    //
                    // AF: This lacks nuance; let's come back to it once we have more tests in place.

                    return Directive.Restart;
                })
            );
        }
    }
}
