using Newtonsoft.Json;

namespace AKDK.Messages.DockerEvents
{
	/// <summary>
    /// 	Represents an event relating to a volume. 
    /// </summary>
	public class VolumeEvent
		: DockerEvent
	{
		/// <summary>
        ///		Initialise the <see cref="VolumeEvent"/>. 
        /// </summary>
		public VolumeEvent(DockerEventType eventType)
            : base(DockerEventTarget.Volume, eventType)
		{
		}

        /// <summary>
        ///     The Id of the volume that the event relates to.
        /// </summary>
        [JsonProperty("id")]
        public string VolumeId { get; set; }
	}
}
