using Newtonsoft.Json;

namespace AKDK.Messages.DockerEvents
{
	/// <summary>
	/// 	Represents an event relating to an image. 
	/// </summary>
	public abstract class ImageEvent
		: DockerEvent
	{
		/// <summary>
		///		Initialise the <see cref="ImageEvent"/>. 
		/// </summary>
		protected ImageEvent()
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

		/// <summary>
		///		The type of entity (e.g. image, container, etc) that the event relates to. 
		/// </summary>
		[JsonProperty("Type")]
		public override DockerEventTarget TargetType => DockerEventTarget.Image;
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
		{
		}

		/// <summary>
		///		The event type.
		/// </summary>
		public sealed override DockerEventType EventType => DockerEventType.Pull;
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
		{
		}

		/// <summary>
		///		The event type.
		/// </summary>
		public override DockerEventType EventType => DockerEventType.Push;
	}
}