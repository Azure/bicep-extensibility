// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    /// <summary>
    /// The JSON serializer context for the V2 models.
    /// </summary>
    [JsonSerializable(typeof(JsonObject))]
    [JsonSerializable(typeof(Error))]
    [JsonSerializable(typeof(ErrorDetail))]
    [JsonSerializable(typeof(ErrorResponse))]
    [JsonSerializable(typeof(Resource))]
    [JsonSerializable(typeof(ResourcePreview))]
    [JsonSerializable(typeof(ResourcePreviewMetadata))]
    [JsonSerializable(typeof(ResourcePreviewSpecification))]
    [JsonSerializable(typeof(ResourcePreviewSpecificationMetadata))]
    [JsonSerializable(typeof(ResourceSpecification))]
    [JsonSerializable(typeof(ResourceReference))]
    [JsonSerializable(typeof(LongRunningOperation))]
    public partial class ModelSerializerContext : JsonSerializerContext
    {
    }
}
