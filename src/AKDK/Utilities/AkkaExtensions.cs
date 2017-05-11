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
        ///     Determine whether another actor is the direct parent of the actor.
        /// </summary>
        /// <param name="actor">
        ///     The actor.
        /// </param>
        /// <param name="otherActor">
        ///     The other actor.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if <paramref name="otherActor"/> is the direct parent of <paramref name="actor"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsParentOf(this IActorRef actor, IActorRef otherActor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (otherActor == null)
                throw new ArgumentNullException(nameof(otherActor));

            return actor.Path.Parent == otherActor.Path;
        }

        /// <summary>
        ///     Determine whether another actor is a direct child of the actor.
        /// </summary>
        /// <param name="actor">
        ///     The actor.
        /// </param>
        /// <param name="otherActor">
        ///     The other actor.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if <paramref name="otherActor"/> is a direct child of <paramref name="actor"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsChildOf(this IActorRef actor, IActorRef otherActor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (otherActor == null)
                throw new ArgumentNullException(nameof(otherActor));

            return otherActor.IsParentOf(actor);
        }

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

            ActorPath actorPath = actor.Path;
            ActorPath otherParentPath = otherActor.Path.Parent;

            while (otherParentPath != otherParentPath.Root)
            {
                if (otherParentPath == actorPath)
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
