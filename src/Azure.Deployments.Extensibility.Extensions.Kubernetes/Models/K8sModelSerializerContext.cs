// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Models;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Models
{
    [JsonSerializable(typeof(K8sObjectIdentifiers))]
    [JsonSerializable(typeof(Resource<K8sObjectIdentifiers, JsonObject>), TypeInfoPropertyName = "K8sResource")]
    internal partial class K8sModelSerializerContext : JsonSerializerContext
    {
        public static K8sModelSerializerContext WithDefaultOptions { get;} = new(JsonDefaults.SerializerOptions);
    }
}
