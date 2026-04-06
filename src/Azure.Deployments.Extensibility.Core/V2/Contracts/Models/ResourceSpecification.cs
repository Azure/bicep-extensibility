// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models
{
    /// <summary>
    /// Represents the desired state of a resource as declared in the user's template.
    /// Used as input to the create-or-update and preview operations.
    /// </summary>
    /// <typeparam name="TProperties">The type representing the resource properties.</typeparam>
    /// <typeparam name="TConfig">The type representing the extension configuration.</typeparam>
    /// <remarks>
    /// See the "Resource Specification" model in <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#resource-specification">contract.md</see>.
    /// </remarks>
    public record ResourceSpecification<TProperties, TConfig>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSpecification{TProperties, TConfig}"/> record.
        /// </summary>
        public ResourceSpecification()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSpecification{TProperties, TConfig}"/> record
        /// with the specified properties.
        /// </summary>
        /// <param name="type">The type of the resource.</param>
        /// <param name="properties">The properties of the resource.</param>
        /// <param name="apiVersion">The API version of the resource.</param>
        /// <param name="config">The configuration for the resource.</param>
        /// <param name="configId">The ID of the configuration for the resource.</param>
        [SetsRequiredMembers]
        public ResourceSpecification(
            string type,
            TProperties properties,
            string? apiVersion = null,
            TConfig? config = default,
            string? configId = null)
        {
            this.Type = type;
            this.Properties = properties;
            this.ApiVersion = apiVersion;
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
    
    /// <inheritdoc cref="ResourceSpecification{TProperties, TConfig}"/>
    /// <typeparam name="TProperties">The type representing the resource properties.</typeparam>
    public record ResourceSpecification<TProperties> : ResourceSpecification<TProperties, JsonObject?>
    {
    }

    /// <summary>
    /// Represents a resource specification with untyped JSON properties and configuration.
    /// </summary>
    /// <inheritdoc cref="ResourceSpecification{TProperties, TConfig}"/>
    public record ResourceSpecification : ResourceSpecification<JsonObject, JsonObject?>
    {
    }
}
