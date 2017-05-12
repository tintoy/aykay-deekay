using Akka.Actor;
using System;
using System.Threading.Tasks;

namespace AKDK
{
    using Actors;
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

            Connected connected = await dockerApi.Manager.Ask<Connected>(
                message: Connect.Local(),
                timeout: connectTimeout ?? TimeSpan.FromSeconds(30)
            );

            return connected.Client;
        }

        /// <summary>
        ///     Request a connection to the local docker API.
        /// </summary>
        /// <param name="dockerApi">
        ///     The Docker API extension for Akka.NET.
        /// </param>
        /// <param name="replyTo">
        ///     The actor to which the reply will be sent.
        /// </param>
        /// <param name="correlationId">
        ///     A message correlation Id that will be returned with the response.
        /// </param>
        /// <remarks>
        ///     If successful, a reference to the <see cref="Client"/> actor will be delivered via a <see cref="Connected"/> message.
        ///     Otherwise, a <see cref="ConnectFailed"/> message will be delivered.
        /// </remarks>
		public static void RequestConnectLocal(this DockerApi dockerApi, IActorRef replyTo = null, string correlationId = null)
        {
            if (dockerApi == null)
                throw new ArgumentNullException(nameof(dockerApi));

            replyTo = replyTo ?? ActorCell.GetCurrentSenderOrNoSender();
            if (replyTo.IsNobody())
                throw new InvalidOperationException("Cannot determine the actor to receive the reply.");

            dockerApi.Manager.Tell(
                Connect.Local(correlationId),
                sender: replyTo
            );
        }        

        /// <summary>
        ///     Connect to a docker API over TCP, waiting for the connection to become available.
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

            Connected connected = await dockerApi.Manager.Ask<Connected>(
                message: Connect.Tcp(hostName, port),
                timeout: connectTimeout ?? TimeSpan.FromSeconds(30)
            );

            return connected.Client;
        }

        /// <summary>
        ///     Request a connection to a docker API over TCP.
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
        /// <param name="replyTo">
        ///     The actor to which the reply will be sent.
        /// </param>
        /// <param name="correlationId">
        ///     A message correlation Id that will be returned with the response.
        /// </param>
        /// <remarks>
        ///     If successful, a reference to the <see cref="Client"/> actor will be delivered via a <see cref="Connected"/> message.
        /// </remarks>
		public static void RequestConnectTcp(this DockerApi dockerApi, string hostName, int port = Connect.DefaultPort, IActorRef replyTo = null, string correlationId = null)
        {
            if (dockerApi == null)
                throw new ArgumentNullException(nameof(dockerApi));

            replyTo = replyTo ?? ActorCell.GetCurrentSenderOrNoSender();
            if (replyTo.IsNobody())
                throw new InvalidOperationException("Cannot determine the actor to receive the reply.");

            dockerApi.Manager.Tell(
                Connect.Local(correlationId),
                sender: replyTo
            );
        }
    }
}
