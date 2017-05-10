using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace AKDK.Messages.DockerEvents.Converters
{
	/// <summary>
	///		JSON converter that that enables custom selection logic during deserialisation of the object to create based on the JSON encountered.
	/// </summary>
	/// <typeparam name="TBase">
	///		The base type of object that the converter can deserialise.
	/// </typeparam>
	public abstract class JsonCreationConverter<TBase>
		: JsonConverter
	{
		/// <summary>
		///		Create a new <see cref="JsonCreationConverter{T}"/>.
		/// </summary>
		protected JsonCreationConverter()
		{
		}

		/// <summary>
		///		Can the converter write JSON?
		/// </summary>
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///		Determine whether the converter can convert an object of the specified type.
		/// </summary>
		/// <param name="objectType">
		///		The object type.
		/// </param>
		/// <returns>
		///		<c>true</c>, if the converter can convert an object of the specified type; otherwise, <c>false</c>.
		/// </returns>
		public override bool CanConvert(Type objectType)
		{
			if (objectType == null)
				throw new ArgumentNullException(nameof(objectType));

			return typeof(TBase).IsAssignableFrom(objectType);
		}

		/// <summary>
		///		Deserialise an object from JSON.
		/// </summary>
		/// <param name="reader">
		///		The <see cref="JsonReader"/> to read from.
		/// </param><param name="objectType">
		///		Type (or base type) of the object being deserialised.
		/// </param><param name="existingValue">
		///		The existing value of the object being read.
		/// </param>
		/// <param name="serializer">
		///		The calling serializer.
		/// </param>
		/// <returns>
		/// The object value.
		/// </returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			if (objectType == null)
				throw new ArgumentNullException(nameof(objectType));

			if (serializer == null)
				throw new ArgumentNullException(nameof(serializer));

			if (reader.TokenType == JsonToken.Null)
				return null;

			if (reader.TokenType != JsonToken.StartObject)
			{
				throw new FormatException(
					String.Format(
						"Unexpected token '{0}' encountered while deserialising object of type '{1}'.",
						reader.TokenType,
						typeof(TBase).FullName
					)
				);
			}

			JObject json = JObject.Load(reader);
			TBase target = Create(objectType, json);
			if (target == null)
				throw new InvalidOperationException("JsonCreationConverter.Create returned null.");

			// Populate the object properties
			serializer.Populate(
				json.CreateReader(),
				target
			);

			return target;
		}

		/// <summary>
		///		Serialise an object to JSON.
		/// </summary>
		/// <param name="writer">
		///		The <see cref="JsonWriter"/> to which serialised object data will be written.
		/// </param>
		/// <param name="value">
		///		The object to serialise.
		/// </param>
		/// <param name="serializer">
		///		The calling serialiser.
		/// </param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));

			if (serializer == null)
				throw new ArgumentNullException(nameof(serializer));

			throw new NotImplementedException("JsonCreationConverter cannot write JSON.");
		}

		/// <summary>
		///		Create an instance of to be populated, based properties in the JSON.
		/// </summary>
		/// <param name="objectType">
		///		type of object expected.
		/// </param>
		/// <param name="json">
		///		The JSON containing serialised object data.
		/// </param>
		/// <returns>
		///		The object instance to be populated.
		/// </returns>
		protected abstract TBase Create(Type objectType, JObject json);
	}
}