// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceReference : ResourceReference<JsonObject, JsonObject>
    {
        public ResourceReference()
            : base()
        {
        }

        [SetsRequiredMembers]
        public ResourceReference(string type, string? apiVersion, JsonObject identifiers, JsonObject? config)
            : base(type, apiVersion, identifiers, config)
        {
        }
    }

    public record ResourceReference<TIdentifiers, TConfig>
    {
        public ResourceReference()
        {
        }

        [SetsRequiredMembers]
        public ResourceReference(string type, string? apiVersion, TIdentifiers identifiers, TConfig? config)
        {
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Identifiers = identifiers;
            this.Config = config;
        }

        public required string Type { get; init; }

        public string? ApiVersion { get; init; }

        public required TIdentifiers Identifiers { get; init; }

        public TConfig? Config { get; init; }
        
        public string? ConfigId { get; init; }
    }
}
