// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models
{
    /// <summary>
    /// Represents a reference to a resource, which contains necessary information to
    /// uniquely identify the resource, including its type, API version, and identifiers,
    /// and optionally a configuration object.
    /// </summary>
    public record ResourceReference
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

        /// <summary>
        /// The type of the resource.
        /// </summary>
        public required string Type { get; init; }

        /// <summary>
        /// The API version of the resource.
        /// </summary>
        public string? ApiVersion { get; init; }

        /// <summary>
        /// The identifier properties. This should be a subset of the resource properties
        /// that can identify the resource.
        /// </summary>
        /// <remarks>
        /// The identifiers themselves may not be sufficient to globally uniquely identify the resource.
        /// If the resource requires configuration, <see cref="ConfigId"/> must also be used along with
        /// the identifiers to uniquely identify the resource.
        /// This is because two resources have with the same identifiers, but with different configurations
        /// which could be targeting different control planes.
        /// For example, two Kubernetes clusters can contain objects with the exact same name and namespace.
        /// </remarks>
        public required JsonObject Identifiers { get; init; }

        /// <summary>
        /// The configuration for the resource.
        /// </summary>
        public JsonObject? Config { get; init; }

        /// <summary>
        /// The ID of the configuration for the resource.
        /// </summary>
        public string? ConfigId { get; init; }
    }
}
