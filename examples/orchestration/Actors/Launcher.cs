using Akka.Actor;
using AKDK.Actors;
using AKDK.Messages.DockerEvents;
using System;
using System.Collections.Generic;

namespace AKDK.Examples.Orchestration.Actors
{
    using Messages;

    /// <summary>
    ///     Actor that launches and manages <see cref="Process"/>es.
    /// </summary>
    public class Launcher
        : ReceiveActorEx
    {
        /// <summary>
        ///     Active processes, keyed by container Id.
        /// </summary>
        readonly Dictionary<string, IActorRef> _activeProcesses = new Dictionary<string, IActorRef>();

        /// <summary>
        ///     Create a new <see cref="Launcher"/> actor.
        /// </summary>
        public Launcher()
        {
        }

        
    }
}
