// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models
{
    /// <summary>
    /// The resource model represents an instance of a resource response
    /// from the /preview, /createOrUpdate, or /get APIs.
    /// </summary>
    public record Resource
    {
        public Resource()
        {
        }

        [SetsRequiredMembers]
        public Resource(
            string type,
            string? apiVersion,
            JsonObject identifiers,
            JsonObject properties,
            JsonObject? config = null,
            string? configId = null,
            string? status = null,
            Error? error = null)
        {
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Identifiers = identifiers;
            this.Properties = properties;
            this.Config = config;
            this.ConfigId = configId;
            this.Status = status;
            this.Error = error;
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
        /// The identifiers that uniquely identify the resource.
        /// </summary>
        public required JsonObject Identifiers { get; init; }

        /// <summary>
        /// The properties of the resource.
        /// </summary>
        public required JsonObject Properties { get; init; }

        /// <summary>
        /// The configuration of the resource, with secrets removed.
        /// The property must be set if the resource has a configuration.
        /// </summary>
        public JsonObject? Config { get; init; }

        /// <summary>
        /// The ID of the configuration for the resource.
        /// The property must be set if the resource has a configuration.
        /// </summary>
        public string? ConfigId { get; init; }

        /// <summary>
        /// The status of the resource.
        /// </summary>
        public string? Status { get; init; } = null;

        /// <summary>
        /// Optional error information if the long-running resource operation failed.
        /// This should only be set if <see cref="Status"/> is "Failed".
        /// </summary>
        public Error? Error { get; init; }
    }
}
