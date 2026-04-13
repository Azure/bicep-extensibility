// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Represents a resource returned by the create-or-update, get, or delete operations.
/// </summary>
/// <typeparam name="TProperties">The type representing the resource properties.</typeparam>
/// <typeparam name="TIdentifiers">The type representing the resource identifiers.</typeparam>
/// <typeparam name="TConfig">The type representing the extension configuration.</typeparam>
/// <remarks>
/// See the "Resource" model in <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#resource">contract.md</see>.
/// </remarks>
public record Resource<TProperties, TIdentifiers, TConfig>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Resource{TProperties, TIdentifiers, TConfig}"/> record.
    /// </summary>
    public Resource()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Resource{TProperties, TIdentifiers, TConfig}"/> record
    /// with the specified properties.
    /// </summary>
    /// <param name="type">The type of the resource.</param>
    /// <param name="identifiers">The identifiers that uniquely identify the resource.</param>
    /// <param name="properties">The properties of the resource.</param>
    /// <param name="apiVersion">The API version of the resource.</param>
    /// <param name="config">The configuration of the resource, with secret properties removed.</param>
    /// <param name="configId">A value that uniquely identifies the deployment control plane.</param>
    /// <param name="status">The provisioning status of the resource.</param>
    /// <param name="error">Error information for a failed long-running operation.</param>
    [SetsRequiredMembers]
    public Resource(
        string type,
        TIdentifiers identifiers,
        TProperties properties,
        string? apiVersion = null,
        TConfig? config = default,
        string? configId = null,
        string? status = null,
        Error? error = null)
    {
        this.Type = type;
        this.Identifiers = identifiers;
        this.Properties = properties;
        this.ApiVersion = apiVersion;
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
    public required TIdentifiers Identifiers { get; init; }

    /// <summary>
    /// The properties of the resource.
    /// </summary>
    public required TProperties Properties { get; init; }

    /// <summary>
    /// The configuration of the resource, with secret properties removed.
    /// Must be included in the response if the resource has a configuration.
    /// Any property not echoed back is treated as a secret by the deployment engine.
    /// </summary>
    public TConfig? Config { get; init; }

    /// <summary>
    /// A value that uniquely identifies the deployment control plane (e.g., a hash of the endpoint URL).
    /// Must be included in the response if the resource has a configuration.
    /// </summary>
    public string? ConfigId { get; init; }

    /// <summary>
    /// The provisioning status of the resource. Terminal values are "Succeeded", "Failed", and "Canceled".
    /// A non-terminal value (e.g., "Running") indicates a RELO long-running operation is in progress.
    /// </summary>
    public string? Status { get; init; } = null;

    /// <summary>
    /// Error information for a failed long-running operation.
    /// Must be set when <see cref="Status"/> is "Failed"; must be omitted otherwise.
    /// </summary>
    public Error? Error { get; init; }
}

/// <inheritdoc cref="Resource{TProperties, TIdentifiers, TConfig}"/>
public record Resource<TProperties, TIdentifiers> : Resource<TProperties, TIdentifiers, JsonObject>
{
}

/// <summary>
/// Represents a resource with untyped JSON properties, identifiers, and configuration.
/// </summary>
/// <inheritdoc cref="Resource{TProperties, TIdentifiers, TConfig}"/>
public record Resource : Resource<JsonObject, JsonObject>
{
}
