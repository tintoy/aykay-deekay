using Newtonsoft.Json;

namespace AKDK.Messages.DockerEvents
{
	/// <summary>
    /// 	Represents an event relating to a container. 
    /// </summary>
	public abstract class ContainerEvent
		: DockerEvent
	{
		/// <summary>
        ///		Initialise the <see cref="ContainerEvent"/>. 
        /// </summary>
		protected ContainerEvent()
		{
		}

		/// <summary>
		/// 	The name of the container that the event relates to.
		/// </summary>
		[JsonIgnore]
		public string Name => GetActorAttribute("name");

		/// <summary>
		/// 	The image used to the container that the event relates to.
		/// </summary>
		[JsonIgnore]
		public string Image => GetActorAttribute("image");
		
		/// <summary>
        ///		The type of entity (e.g. image, container, etc) that the event relates to. 
        /// </summary>
		[JsonProperty("Type")]
		public sealed override DockerEventTarget TargetType => DockerEventTarget.Container;
	}

	/// <summary>
    ///		Event raised when a container is created. 
    /// </summary>
	public class ContainerCreated
		: ContainerEvent
	{
		/// <summary>
        ///		Create a new <see cref="ContainerCreated"/> event model. 
        /// </summary>
		public ContainerCreated()
		{
		}

		/// <summary>
        /// 	The event type.
        /// </summary>
		[JsonProperty("status")]
		public override DockerEventType EventType => DockerEventType.Create;
	}

	/// <summary>
    ///		Model for the event raised when a container has been terminated. 
    /// </summary>
	public class ContainerDied
		: ContainerEvent
	{
		/// <summary>
        ///		Create a new <see cref="ContainerDied"/> event model. 
        /// </summary>
		public ContainerDied()
		{
		}

		/// <summary>
        /// 	The event type.
        /// </summary>
		[JsonProperty("status")]
		public override DockerEventType EventType => DockerEventType.Die;
	}
}
