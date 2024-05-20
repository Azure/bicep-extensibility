// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record Resource
    {
        public Resource()
        {
        }

        [SetsRequiredMembers]
        public Resource(string type, string? apiVersion, JsonObject identifiers, JsonObject properties, string? status)
            
        {
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Identifiers = identifiers;
            this.Properties = properties;
            this.Status = status;
        }

        public required string Type { get; init; }

        public string? ApiVersion { get; init; }

        public required JsonObject Identifiers { get; init; }

        public required JsonObject Properties { get; init; }

        public string? Status { get; init; }
    }
}
