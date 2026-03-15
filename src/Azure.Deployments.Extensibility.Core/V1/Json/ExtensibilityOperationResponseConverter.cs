// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Azure.Deployments.Extensibility.Core.Json
{
    public class ExtensibilityOperationResponseConverter : JsonConverter<ExtensibilityOperationResponse>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(ExtensibilityOperationResponse).IsAssignableFrom(typeToConvert);
        }

        public override ExtensibilityOperationResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var element = document.RootElement;

            return element.TryGetProperty(nameof(ExtensibilityOperationErrorResponse.Errors).ToLowerInvariant(), out var _)
                ? ExtensibilityJsonSerializer.WithoutConverters.Deserialize<ExtensibilityOperationErrorResponse>(element)
                : ExtensibilityJsonSerializer.WithoutConverters.Deserialize<ExtensibilityOperationSuccessResponse>(element);
        }

        public override void Write(Utf8JsonWriter writer, ExtensibilityOperationResponse value, JsonSerializerOptions options)
        {
            ExtensibilityJsonSerializer.WithoutConverters.Serialize(writer, (object)value);
        }
    }
}
