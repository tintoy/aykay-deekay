using Akka.Actor;
using System;

namespace AKDK.Utilities
{
    /// <summary>
    ///     Extension methods for Akka types.
    /// </summary>
    public static class AkkaExtensions
    {
        /// <summary>
        ///     Determine whether another actor is a descendent of the actor.
        /// </summary>
        /// <param name="actor">
        ///     The actor.
        /// </param>
        /// <param name="otherActor">
        ///     The other actor.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if <paramref name="otherActor"/> is a descendant of <paramref name="actor"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDescendantOf(this IActorRef actor, IActorRef otherActor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (otherActor == null)
                throw new ArgumentNullException(nameof(otherActor));

            ActorPath parentPath = actor.Path;
            ActorPath otherParentPath = otherActor.Path.Parent;
            ActorPath rootPath = otherActor.Path.Root;
            while (otherParentPath != rootPath)
            {
                if (otherParentPath == parentPath)
                    return true;

                otherParentPath = otherParentPath.Parent;
            }

            return false;
        }

        /// <summary>
        ///     Determine whether another actor is an ancestor of the actor.
        /// </summary>
        /// <param name="actor">
        ///     The actor.
        /// </param>
        /// <param name="otherActor">
        ///     The other actor.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if <paramref name="otherActor"/> is an ancestor of <paramref name="actor"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAncestorOf(this IActorRef actor, IActorRef otherActor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (otherActor == null)
                throw new ArgumentNullException(nameof(otherActor));

            return otherActor.IsDescendantOf(actor);
        }
    }
}
