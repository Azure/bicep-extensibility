// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Represents a reference to a resource. Contains the information needed to uniquely identify
/// a resource, including its type, API version, identifiers, and optionally a configuration object.
/// Used as input to the get and delete operations.
/// </summary>
/// <typeparam name="TIdentifiers">The type representing the resource identifiers.</typeparam>
/// <typeparam name="TConfig">The type representing the extension configuration.</typeparam>
/// <remarks>
/// See the "Resource Reference" model in <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#resource-reference">contract.md</see>.
/// </remarks>
public record ResourceReference<TIdentifiers, TConfig>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceReference{TIdentifiers, TConfig}"/> record.
    /// </summary>
    public ResourceReference()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceReference{TIdentifiers, TConfig}"/> record
    /// with the specified properties.
    /// </summary>
    /// <param name="type">The type of the resource.</param>
    /// <param name="identifiers">The identifiers that uniquely identify the resource.</param>
    /// <param name="apiVersion">The API version of the resource.</param>
    /// <param name="config">The configuration for the resource.</param>
    /// <param name="configId">A checksum that identifies the configuration.</param>
    [SetsRequiredMembers]
    public ResourceReference(
        string type,
        TIdentifiers identifiers,
        string? apiVersion = null,
        TConfig? config = default,
        string? configId = null)
    {
        this.Type = type;
        this.Identifiers = identifiers;
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
    public required TIdentifiers Identifiers { get; init; }

    /// <summary>
    /// The configuration for the resource.
    /// </summary>
    public TConfig? Config { get; init; }

    /// <summary>
    /// A checksum that identifies the configuration. Required for delete operations when
    /// a configuration is present. If provided, the extension must validate it.
    /// </summary>
    public string? ConfigId { get; init; }
}

/// <inheritdoc cref="ResourceReference{TIdentifiers, TConfig}"/>
public record ResourceReference<TIdentifiers> : ResourceReference<TIdentifiers, JsonObject>
{
}

/// <summary>
/// Represents a resource reference with untyped JSON identifiers and configuration.
/// </summary>
/// <inheritdoc cref="ResourceReference{TIdentifiers, TConfig}"/>
public record ResourceReference : ResourceReference<JsonObject>
{
}

