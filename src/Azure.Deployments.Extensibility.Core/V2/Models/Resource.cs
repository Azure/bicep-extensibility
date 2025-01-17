// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record Resource : Resource<JsonObject, JsonObject>
    {
        public Resource()
            : base()
        {
        }

        [SetsRequiredMembers]
        public Resource(string type, string? apiVersion, JsonObject identifiers, JsonObject properties, string? configId = null, string? status = null)
            : base(type, apiVersion, identifiers, properties, configId, status)
        {
        }
    }

    public record Resource<TIdentifiers, TProperties>
    {
        public Resource()
        {
        }

        [SetsRequiredMembers]
        public Resource(string type, string? apiVersion, TIdentifiers identifiers, TProperties properties, string? configId = null, string? status = null)
        {
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Identifiers = identifiers;
            this.Properties = properties;
            this.ConfigId = configId;
            this.Status = status;
        }

        public required string Type { get; init; }

        public string? ApiVersion { get; init; }

        public required TIdentifiers Identifiers { get; init; }

        public required TProperties Properties { get; init; }

        public string? Status { get; init; } = null;

        public string? ConfigId { get; init; }
    }
}
