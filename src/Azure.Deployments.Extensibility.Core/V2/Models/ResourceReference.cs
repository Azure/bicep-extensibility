// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public class ResourceReference
    {
        public ResourceReference()
        {
        }

        [SetsRequiredMembers]
        public ResourceReference(string type, string? apiVersion, JsonObject identifiers, JsonObject? config)
        {
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Identifiers = identifiers;
            this.Config = config;
        }

        public required string Type { get; init; }

        public string? ApiVersion { get; init; }

        public required JsonObject Identifiers { get; init; }

        public JsonObject? Config { get; init; }
    }
}
