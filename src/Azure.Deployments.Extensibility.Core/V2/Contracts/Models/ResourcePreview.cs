// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Represents a preview of a resource, returned from the preview operation.
/// The preview reflects what a get response would return if the create-or-update operation had been performed.
/// </summary>
/// <typeparam name="TProperties">The type representing the resource properties.</typeparam>
/// <typeparam name="TIdentifiers">The type representing the resource identifiers.</typeparam>
/// <typeparam name="TConfig">The type representing the extension configuration.</typeparam>
/// <remarks>
/// See the "Resource Preview" model in <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#resource-preview">contract.md</see>
/// and <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/preview-operation.md">preview-operation.md</see>.
/// </remarks>
public record ResourcePreview<TProperties, TIdentifiers, TConfig>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourcePreview{TProperties, TIdentifiers, TConfig}"/> record.
    /// </summary>
    public ResourcePreview()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourcePreview{TProperties, TIdentifiers, TConfig}"/> record
    /// with the specified properties.
    /// </summary>
    /// <param name="type">The type of the resource.</param>
    /// <param name="identifiers">The identifiers that uniquely identify the resource.</param>
    /// <param name="properties">The properties of the resource.</param>
    /// <param name="apiVersion">The API version of the resource.</param>
    /// <param name="status">The status of the resource.</param>
    /// <param name="config">The configuration of the resource, with secret properties removed.</param>
    /// <param name="configId">A value that uniquely identifies the deployment control plane.</param>
    /// <param name="metadata">The metadata describing property classifications in the preview.</param>
    [SetsRequiredMembers]
    public ResourcePreview(
        string type,
        TIdentifiers identifiers,
        TProperties properties,
        string? apiVersion = null,
        string? status = null,
        TConfig? config = default,
        string? configId = null,
        ResourcePreviewMetadata? metadata = null)
    {
        this.Type = type;
        this.Identifiers = identifiers;
        this.Properties = properties;
        this.ApiVersion = apiVersion;
        this.Status = status;
        this.Config = config;
        this.ConfigId = configId;
        this.Metadata = metadata;
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
    public required TIdentifiers Identifiers { get; init; }

    /// <summary>
    /// The properties of the resource.
    /// </summary>
    public required TProperties Properties { get; init; }

    /// <summary>
    /// The status of the resource.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// The configuration of the resource, with secret properties removed.
    /// Must be included in the response if the resource has a configuration.
    /// </summary>
    public TConfig? Config { get; init; }

    /// <summary>
    /// A value that uniquely identifies the deployment control plane.
    /// Must be included in the response if the resource has a configuration.
    /// </summary>
    public string? ConfigId { get; init; }

    /// <summary>
    /// The metadata describing which properties in the preview are read-only, immutable, calculated, or unevaluated.
    /// </summary>
    public ResourcePreviewMetadata? Metadata { get; init; }
}

/// <inheritdoc cref="ResourcePreview{TProperties, TIdentifiers, TConfig}"/>
public record ResourcePreview<TProperties, TIdentifiers> : ResourcePreview<TProperties, TIdentifiers, JsonObject>
{
}

/// <summary>
/// Represents a resource preview with untyped JSON properties, identifiers, and configuration.
/// </summary>
/// <inheritdoc cref="ResourcePreview{TProperties, TIdentifiers, TConfig}"/>
public record ResourcePreview : ResourcePreview<JsonObject, JsonObject>
{
}
