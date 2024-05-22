// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.V2.Json
{
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ErrorData))]
    [JsonSerializable(typeof(Resource))]
    [JsonSerializable(typeof(ResourceSpecification))]
    [JsonSerializable(typeof(ResourceReference))]
    [JsonSerializable(typeof(ResourceOperation))]
    public partial class DefaultJsonSerializerContext : JsonSerializerContext
    {
        public static void ConfigureSerializerOptions(JsonSerializerOptions options)
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            options.TypeInfoResolverChain.Add(Default);
        }
    }
}
