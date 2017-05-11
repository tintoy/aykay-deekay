using Newtonsoft.Json;

namespace AKDK.Messages.DockerEvents
{
	/// <summary>
	/// 	Represents an event relating to an image. 
	/// </summary>
	public class ImageEvent
		: DockerEvent
	{
		/// <summary>
		///		Initialise the <see cref="ImageEvent"/>. 
		/// </summary>
		public ImageEvent(DockerEventType eventType)
            : base(DockerEventTarget.Image, eventType)
		{
		}

		/// <summary>
		/// 	The full name (e.g. "alpine:latest") of the image that the event relates to.
		/// </summary>
		[JsonIgnore]
		public string Name => Actor.Id;

		/// <summary>
		///		The base name (e.g. "alpine") of the image that the event relates to.
		/// </summary>
		[JsonIgnore]
		public string BaseName => GetActorAttribute("name");
	}

	/// <summary>
	///		Model for the event raised when an image has been successfully pulled.
	/// </summary>
	public class ImagePulled
		: ImageEvent
	{
		/// <summary>
		///		Create a new <see cref="ImagePulled"/> event model.
		/// </summary>
		public ImagePulled()
            : base(DockerEventType.Pull)
		{
		}
	}

	/// <summary>
	///		Model for the event raised when an image has been successfully pushed.
	/// </summary>
	public class ImagePushed
		: ImageEvent
	{
		/// <summary>
		///		Create a new <see cref="ImagePulled"/> event model.
		/// </summary>
		public ImagePushed()
            : base(DockerEventType.Pull)
        {
		}
	}
}