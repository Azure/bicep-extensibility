// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public class ResourceSpecification
    {
        public ResourceSpecification()
        {
        }

        [SetsRequiredMembers]
        public ResourceSpecification(string type, string? apiVersion, JsonObject properties, JsonObject? config)
        {
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Properties = properties;
            this.Config = config;
        }

        public required string Type { get; init; }

        public string? ApiVersion { get; init; }

        public required JsonObject Properties { get; init; }

        public JsonObject? Config { get; init; }
    }
}
