// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models
{
    /// <summary>
    /// Represents what the user declares in their template to specify the desired state of a resource.
    /// </summary>
    public record ResourceSpecification
    {
        public ResourceSpecification()
        {
        }

        [SetsRequiredMembers]
        public ResourceSpecification(string type, string? apiVersion, JsonObject properties, JsonObject? config, string? configId = null)
        {
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Properties = properties;
            this.Config = config;
            this.ConfigId = configId;
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
        /// The properties of the resource.
        /// </summary>
        public required JsonObject Properties { get; init; }

        /// <summary>
        /// The configuration for the resource.
        /// </summary>
        public JsonObject? Config { get; init; }

        /// <summary>
        /// The ID of the configuration for the resource.
        /// </summary>
        /// <remarks>
        /// This is only set when the request is sent from the Deployment Stacks service.
        /// Extensions should calculate the config ID based on the config and compare it
        /// with this value. If they don't match, the extension should reject the request.
        /// This is to prevent operations from being performed on the wrong target,
        /// which can happen if the user deleted the original resource and recreated it
        /// with the same properties but different config.
        /// For example, the Kubernetes extension uses the config ID to ensure that operations
        /// are performed on the correct cluster.
        /// </remarks>
        public string? ConfigId { get; init; }
    }
}
