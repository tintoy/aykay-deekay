using Akka.Actor;
using System;
using System.Threading.Tasks;

namespace AKDK
{
	using Messages;

	/// <summary>
	///		Extension methods for the <see cref="DockerApi">Docker API</see>.
	/// </summary>
	public static class DockerApiExtensions
	{
        /// <summary>
        ///     Connect to the local docker API.
        /// </summary>
        /// <param name="dockerApi">
        ///     The Docker API extension for Akka.NET.
        /// </param>
        /// <param name="connectTimeout">
        ///     The connection timeout.
        /// </param>
        /// <returns>
        ///     A reference to the client actor for the local Docker API.
        /// </returns>
		public static async Task<IActorRef> ConnectLocal(this DockerApi dockerApi, TimeSpan? connectTimeout = null)
        {
            if (dockerApi == null)
                throw new ArgumentNullException(nameof(dockerApi));

            Connected connected = await dockerApi.ConnectionManager.Ask<Connected>(
                message: Connect.Local(),
                timeout: connectTimeout ?? TimeSpan.FromSeconds(30)
            );

            return connected.Client;
        }

        /// <summary>
        ///     Connect to a docker API over TCP.
        /// </summary>
        /// <param name="dockerApi">
        ///     The Docker API extension for Akka.NET.
        /// </param>
        /// <param name="hostName">
        ///     The name of the target host.
        /// </param>
        /// <param name="port">
        ///     The target TCP port.
        /// </param>
        /// <param name="connectTimeout">
        ///     The connection timeout.
        /// </param>
        /// <returns>
        ///     A reference to the client actor for the target Docker API.
        /// </returns>
		public static async Task<IActorRef> ConnectTcp(this DockerApi dockerApi, string hostName, int port = Connect.DefaultPort, TimeSpan? connectTimeout = null)
        {
            if (dockerApi == null)
                throw new ArgumentNullException(nameof(dockerApi));

            Connected connected = await dockerApi.ConnectionManager.Ask<Connected>(
                message: Connect.Tcp(hostName, port),
                timeout: connectTimeout ?? TimeSpan.FromSeconds(30)
            );

            return connected.Client;
        }
    }
}
