// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceRequestBody
    {
        public ResourceRequestBody()
        {
        }

        [SetsRequiredMembers]
        public ResourceRequestBody(string type, JsonObject properties, JsonObject? config = null)
        {
            this.Type = type;
            this.Properties = properties;
            this.Config = config;
        }

        public required string Type { get; init; }

        public required JsonObject Properties { get; init; }

        public JsonObject? Config { get; init; }
    }
}
