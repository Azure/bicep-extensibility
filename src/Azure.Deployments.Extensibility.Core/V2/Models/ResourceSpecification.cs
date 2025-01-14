// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceSpecification : ResourceSpecification<JsonObject, JsonObject>
    {
        public ResourceSpecification()
            : base()
        {
        }

        [SetsRequiredMembers]
        public ResourceSpecification(string type, string? apiVersion, JsonObject properties, JsonObject? config, string? configId = null)
            : base(type, apiVersion, properties, config, configId)
        {
        }
    }

    public record ResourceSpecification<TProperties, TConfig>
    {
        public ResourceSpecification()
        {
        }

        [SetsRequiredMembers]
        public ResourceSpecification(string type, string? apiVersion, TProperties properties, TConfig? config, string? configId = null)
        {
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Properties = properties;
            this.Config = config;
            this.ConfigId = configId;
        }

        public required string Type { get; init; }

        public string? ApiVersion { get; init; }

        public required TProperties Properties { get; init; }

        public TConfig? Config { get; init; }

        public string? ConfigId { get; init; }
    }
}
