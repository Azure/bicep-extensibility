// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.V2.Json
{
    /// <summary>
    /// Provides default JSON serialization options and a source-generated serializer context for the extensibility API models.
    /// </summary>
    public static class JsonDefaults
    {
        /// <summary>
        /// Default <see cref="JsonSerializerOptions"/> used for serializing and deserializing extensibility API models.
        /// Uses camelCase naming, ignores null values, and uses relaxed JSON escaping.
        /// </summary>
        public readonly static JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>
        /// A source-generated <see cref="ModelSerializerContext"/> configured with <see cref="SerializerOptions"/>.
        /// </summary>
        public readonly static ModelSerializerContext SerializerContext = new(new(SerializerOptions));
    }
}
