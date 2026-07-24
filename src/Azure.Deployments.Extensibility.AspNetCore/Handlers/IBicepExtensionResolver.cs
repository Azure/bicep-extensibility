// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// Resolves the Bicep extension registration that handles an exact extension version from the request route.
/// </summary>
public interface IBicepExtensionResolver
{
    /// <summary>
    /// Resolves <paramref name="extensionVersion"/>, or returns <see langword="null"/> when the version
    /// is malformed or unsupported.
    /// </summary>
    BicepExtensionRegistration? Resolve(string extensionVersion);
}
