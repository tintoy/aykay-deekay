using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace AKDK.Messages.DockerEvents
{
    using Converters;
    using System.IO;

    /// <summary>
    ///		The base model for Docker event data. 
    /// </summary>
    [JsonConverter(typeof(Converters.DockerEventConverter))]
	public class DockerEvent
        : ICorrelatedMessage
	{
        /// <summary>
        ///     Create a new <see cref="DockerEvent"/>.
        /// </summary>
		public DockerEvent(DockerEventTarget targetType, DockerEventType eventType)
        {
            TargetType = targetType;
            EventType = eventType;
		}

        /// <summary>
        ///     The message correlation Id.
        /// </summary>
        public string CorrelationId { get; internal set; }

		/// <summary>
        /// 	The Id of the entity that the event relates to.
        /// </summary>
		[JsonProperty("id")]
		public string Target { get; set; }

		/// <summary>
        /// 	The type of entity (e.g. image, container, etc) that the event relates to.
        /// </summary>
        [JsonIgnore]
		public DockerEventTarget TargetType { get; }

        /// <summary>
        /// 	The event type.
        /// </summary>
        [JsonIgnore]
        public DockerEventType EventType { get; }

		/// <summary>
        /// 	Information about the entity that the event relates to. 
        /// </summary>
		[JsonProperty("Actor", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
		public DockerEventActor Actor { get; } = new DockerEventActor();

		/// <summary>
        /// 	The UTC date / time that the event was raised.
        /// </summary>
		[JsonProperty("time")]
        [JsonConverter(typeof(DockerDateConverter))]
		public DateTime TimestampUTC { get; set; }

		/// <summary>
        ///		Get the value of the event actor attribute with the specified name. 
        /// </summary>
        /// <param name="name">
		/// 	The attribute name.
		/// </param>
        /// <returns>
		/// 	The attribute value (or <c>null</c>, if the attribute is not defined).
		/// </returns>
		public string GetActorAttribute(string name)
		{
			string value;
			Actor.Attributes.TryGetValue(name, out value);

			return value;
		}

        /// <summary>
        ///     Configuration for the event JSON serialiser.
        /// </summary>
        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Converters =
            {
                new StringEnumConverter(),
                new DockerEventConverter()
            }
        };

        /// <summary>
        ///     Serialise the event to JSON.
        /// </summary>
        /// <param name="writer">
        ///     The <see cref="JsonWriter"/> used to write the serialised event data.
        /// </param>
        public void ToJson(JsonWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            JsonSerializer.Create(SerializerSettings).Serialize(writer, this);
        }

        /// <summary>
        ///     Deserialise a docker event from JSON.
        /// </summary>
        /// <param name="json">
        ///     The JSON.
        /// </param>
        /// <returns>
        ///     The deserialised docker event.
        /// </returns>
        public static DockerEvent FromJson(string json)
        {
            if (String.IsNullOrWhiteSpace(json))
                throw new ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(json)}.", nameof(json));

            return JsonConvert.DeserializeObject<DockerEvent>(json);
        }
	}

	/// <summary>
    ///		Information about an entity that a Docker event relates to. 
    /// </summary>
	public class DockerEventActor
	{
		/// <summary>
        ///		The entity Id. 
        /// </summary>
		[JsonProperty("ID")]
		public string Id { get; set; }

		/// <summary>
        ///		The entity attributes (if any). 
        /// </summary>
		[JsonProperty("Attributes", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
		public Dictionary<string, string> Attributes { get; } = new Dictionary<string, string>();
	}
}