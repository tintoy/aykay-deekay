using Akka.Actor;
using System;

namespace AKDK
{
	using Actors;

	/// <summary>
	///		Extension provider for Docker.
	/// </summary>
	class DockerApiProvider :
		ExtensionIdProvider<DockerApi>
	{
		/// <summary>
		///		The singleton instance of the Docker extension provider.
		/// </summary>
		public static readonly DockerApiProvider Instance = new DockerApiProvider();

		/// <summary>
		///		Create a new Docker extension provider.
		/// </summary>
		DockerApiProvider()
		{
		}

		/// <summary>
		///		Create an instance of the extension.
		/// </summary>
		/// <param name="system">
		///		The actor system being extended.
		/// </param>
		/// <returns>
		///		The extension.
		/// </returns>
		public override DockerApi CreateExtension(ExtendedActorSystem system)
		{
			if (system == null)
				throw new ArgumentNullException(nameof(system));
			
			IActorRef manager = system.ActorOf(
				Props.Create<DockerConnectionManager>(),
				name: DockerConnectionManager.ActorName
			);

			return new DockerApi(system, manager);
		}
	}
}