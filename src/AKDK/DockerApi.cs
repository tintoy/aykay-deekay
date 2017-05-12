using Akka.Actor;
using System;

namespace AKDK
{
    /// <summary>
    ///		The Docker extension for Akka actor systems.
    /// </summary>
    public sealed class DockerApi
        : IExtension
    {
        /// <summary>
        ///		Create a new Docker actor system extension.
        /// </summary>
        /// <param name="system">
        ///		The actor system extended by the API.
        /// </param>
        /// <param name="manager">
        ///		A reference to the root Docker management actor.
        /// </param>
        public DockerApi(ActorSystem system, IActorRef manager)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            System = system;
            Manager = manager;
        }

        /// <summary>
        ///		The actor system extended by the API.
        /// </summary>

        public ActorSystem System { get; }

        /// <summary>
        ///		A reference to the root Docker management actor.
        /// </summary>

        internal IActorRef Manager { get; }
    }
}
