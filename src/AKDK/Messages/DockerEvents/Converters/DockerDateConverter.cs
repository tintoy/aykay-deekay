/*
 * Credit where credit is due - this class (DockerDateConverter) was inspired by the implementation of an internal class in Docker.DotNet:
 *
 * https://github.com/Microsoft/Docker.DotNet/blob/master/Docker.DotNet/JsonIso8601AndUnixEpochDateConverter.cs
 *
 * License here: https://github.com/Microsoft/Docker.DotNet/blob/master/LICENSE
 */

using Newtonsoft.Json;
using System;

namespace AKDK.Messages.DockerEvents.Converters
{
    class DockerDateConverter
		: JsonConverter
    {
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (DateTime) || objectType == typeof (DateTime?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
			if (reader.TokenType != JsonToken.Integer)
				throw new NotSupportedException($"{nameof(DockerDateConverter)} cannot convert JSON token of type {reader.TokenType} (expected {JsonToken.Integer}).");

			// Let's keep this implementation simple for now - should be just enough to parse the JSON for Docker events (nothing more). 
            DateTime result = UnixEpoch.AddSeconds((long)reader.Value);
			if (objectType == typeof(DateTime?) && result == default(DateTime))
				return null;

			return result;
        }

		public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}