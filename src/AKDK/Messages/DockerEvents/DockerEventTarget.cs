using System.Runtime.Serialization;

namespace AKDK.Messages.DockerEvents
{
	/// <summary>
    ///		Represents a target for events from the Docker API. 
    /// </summary>
	public enum DockerEventTarget
	{
		/// <summary>
		///		An unknown event source.
		/// </summary>
		Unknown = 0,

		// TODO: Document these.

		/// <summary>
		///		Event relates to a container.
		/// </summary>
		[EnumMember(Value = "container")]
		Container,
		
		/// <summary>
		///		Event relates to an image.
		/// </summary>
		[EnumMember(Value = "image")]
		Image,

		/// <summary>
		///		Event relates to a network.
		/// </summary>
		[EnumMember(Value = "network")]
		Network,
		
		/// <summary>
		///		Event relates to a volume.
		/// </summary>
		[EnumMember(Value = "volume")]
		Volume
	}
}
