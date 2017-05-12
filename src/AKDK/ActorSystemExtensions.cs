using Akka.Actor;
using System;

namespace AKDK
{
	/// <summary>
	///		Extension methods for <see cref="ActorSystem"/>.
	/// </summary>
	public static class ActorSystemExtensions
	{
		/// <summary>
		///		Get the Docker API for the actor system.
		/// </summary>
		/// <param name="system">
		///		The actor system.
		/// </param>
		/// <returns>
		///		The Docker API.
		/// </returns>
		public static DockerApi Docker(this ActorSystem system)
		{
			if (system == null)
				throw new ArgumentNullException(nameof(system));

			return DockerApiProvider.Instance.Apply(system);
		}

        /// <summary>
        ///     Get the Docker API for the current actor context.
        /// </summary>
        /// <param name="context">
        ///     The current actor context
        /// </param>
        /// <returns>
        ///     The Docker API.
        /// </returns>
        public static DockerApi Docker(this IActorContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return context.System.Docker();
        }
    }
}
