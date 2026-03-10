// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models
{
    public record ResourceSpecification<TProperties, TConfig>
    {
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
        public required TProperties Properties { get; init; }

        /// <summary>
        /// The configuration for the resource.
        /// </summary>
        public TConfig? Config { get; init; }

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
    
    public record ResourceSpecification<TProperties> : ResourceSpecification<TProperties, JsonObject?>
    {
    }
    
    public record ResourceSpecification : ResourceSpecification<JsonObject, JsonObject?>
    {
    }
}
