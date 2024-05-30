// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    [JsonSerializable(typeof(JsonObject))]
    [JsonSerializable(typeof(ErrorData))]
    [JsonSerializable(typeof(Resource))]
    [JsonSerializable(typeof(ResourceSpecification))]
    [JsonSerializable(typeof(ResourceReference))]
    [JsonSerializable(typeof(ResourceOperation))]
    public partial class ModelSerializerContext : JsonSerializerContext
    {
    }
}
