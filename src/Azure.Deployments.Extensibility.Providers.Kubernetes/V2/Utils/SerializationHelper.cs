// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Constants;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Utils
{
    public static class SerializationHelper
    {
        public static JsonObject SerializeToJsonObject(object properties) =>
            JsonSerializer.SerializeToNode(properties, JsonDefaults.SerializerOptions)?.AsObject()
            ?? throw new InvalidOperationException($"Cannot serialize {nameof(properties)} to JsonObject.");
    }
}
