// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceSpecification : ResourceSpecification<JsonObject, JsonObject, ResourceMetadata>
    {
        public ResourceSpecification()
            : base()
        {
        }

        [SetsRequiredMembers]
        public ResourceSpecification(string type, string? apiVersion, JsonObject properties, JsonObject? config, string? configId = null, ResourceMetadata? metadata = null)
            : base(type, apiVersion, properties, config, configId, metadata)
        {
        }
    }

    public record ResourceSpecification<TProperties, TConfig, TMetadata>
    {
        public ResourceSpecification()
        {
        }

        [SetsRequiredMembers]
        public ResourceSpecification(string type, string? apiVersion, TProperties properties, TConfig? config, string? configId = null, TMetadata? metadata = default)
        {
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Properties = properties;
            this.Config = config;
            this.ConfigId = configId;
            this.Metadata = metadata;
        }

        public required string Type { get; init; }

        public string? ApiVersion { get; init; }

        public required TProperties Properties { get; init; }

        public TConfig? Config { get; init; }

        public string? ConfigId { get; init; }

        public TMetadata? Metadata { get; init; }
    }
}
