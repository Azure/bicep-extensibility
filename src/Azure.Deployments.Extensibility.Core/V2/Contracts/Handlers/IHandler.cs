// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Semver;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// The base interface for all handlers in the extensibility API.
/// Each handler must specify the range of API versions it supports through the SupportedVersions property.
/// </summary>
public interface IHandler
{
    /// <summary>
    /// Gets the range of semantic extension versions that are supported by the handler.
    /// </summary>
    SemVersionRange SupportedExtensionVersions { get; }
}
