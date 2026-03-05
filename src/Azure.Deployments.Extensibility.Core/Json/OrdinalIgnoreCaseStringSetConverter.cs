// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.Json
{
    public class OrdinalIgnoreCaseStringSetConverter : JsonConverter<ISet<string>>
    {
        public override ISet<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected start of array.");
            }

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndArray:
                        return set;
                    case JsonTokenType.String:
                    {
                        var value = reader.GetString();

                        if (value != null)
                        {
                            set.Add(value);
                        }

                        break;
                    }
                    default:
                        throw new JsonException($"Expected string value in array but received a '{reader.TokenType}' token.");
                }
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, ISet<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var item in value)
            {
                writer.WriteStringValue(item);
            }

            writer.WriteEndArray();
        }
    }
}
