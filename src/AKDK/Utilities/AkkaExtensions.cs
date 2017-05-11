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
        ///     Determine whether another actor path is the direct parent of the actor path.
        /// </summary>
        /// <param name="actorPath">
        ///     The actor path.
        /// </param>
        /// <param name="otherActorPath">
        ///     The other actor path.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if <paramref name="otherActorPath"/> is the direct parent of <paramref name="actorPath"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsParentOf(this ActorPath actorPath, ActorPath otherActorPath)
        {
            if (actorPath == null)
                throw new ArgumentNullException(nameof(actorPath));

            if (otherActorPath == null)
                throw new ArgumentNullException(nameof(otherActorPath));

            return actorPath.Parent == otherActorPath;
        }

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

            return actor.Path.IsParentOf(otherActor.Path);
        }

        /// <summary>
        ///     Determine whether another actor path is the direct child of the actor path.
        /// </summary>
        /// <param name="actorPath">
        ///     The actor path.
        /// </param>
        /// <param name="otherActorPath">
        ///     The other actor path.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if <paramref name="otherActorPath"/> is the direct child of <paramref name="actorPath"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsChildOf(this ActorPath actorPath, ActorPath otherActorPath)
        {
            if (actorPath == null)
                throw new ArgumentNullException(nameof(actorPath));

            if (otherActorPath == null)
                throw new ArgumentNullException(nameof(otherActorPath));

            return otherActorPath.IsParentOf(actorPath);
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
        /// <param name="actorPath">
        ///     The actor.
        /// </param>
        /// <param name="otherActorPath">
        ///     The other actor.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if <paramref name="otherActorPath"/> is a descendant of <paramref name="actorPath"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDescendantOf(this ActorPath actorPath, ActorPath otherActorPath)
        {
            if (actorPath == null)
                throw new ArgumentNullException(nameof(actorPath));

            if (otherActorPath == null)
                throw new ArgumentNullException(nameof(otherActorPath));

            ActorPath otherParentPath = otherActorPath.Parent;

            while (otherParentPath != otherParentPath.Root)
            {
                if (otherParentPath == actorPath)
                    return true;

                otherParentPath = otherParentPath.Parent;
            }

            return false;
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

            return actor.Path.IsDescendantOf(otherActor.Path);
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
