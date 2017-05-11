using Newtonsoft.Json;

namespace AKDK.Messages.DockerEvents
{
	/// <summary>
    /// 	Represents an event relating to a network. 
    /// </summary>
	public class NetworkEvent
		: DockerEvent
	{
		/// <summary>
        ///		Initialise the <see cref="NetworkEvent"/>. 
        /// </summary>
		public NetworkEvent(DockerEventType eventType)
            : base(DockerEventTarget.Network, eventType)
		{
		}

		/// <summary>
		/// 	The name of the network that the event relates to.
		/// </summary>
		[JsonIgnore]
		public string Name => GetActorAttribute("name");

		/// <summary>
		/// 	The container that the event relates to.
		/// </summary>
		[JsonIgnore]
		public string ContainerId => GetActorAttribute("container");
	}

    /// <summary>
    ///		Event raised when a network is connected to a container. 
    /// </summary>
	public class NetworkConnected
        : NetworkEvent
    {
        /// <summary>
        ///		Create a new <see cref="NetworkDisconnected"/> event model. 
        /// </summary>
        public NetworkConnected()
            : base(DockerEventType.Connect)
        {
        }
    }

    /// <summary>
    ///		Event raised when a network is disconnected from a container. 
    /// </summary>
    public class NetworkDisconnected
		: NetworkEvent
	{
        /// <summary>
        ///		Create a new <see cref="NetworkDisconnected"/> event model. 
        /// </summary>
        public NetworkDisconnected()
            : base(DockerEventType.Disconnect)
		{
		}
	}
}
