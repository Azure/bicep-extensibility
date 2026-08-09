// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace BlobPocExtension.Storage;

/// <summary>
/// Bound from the "Storage" configuration section.
/// </summary>
public sealed class BlobPocOptions
{
    /// <summary>
    /// Endpoint suffix used to build the account URL <c>https://{account}.{suffix}</c>.
    /// Defaults to public cloud; override for sovereign clouds.
    /// </summary>
    public string? BlobEndpointSuffix { get; set; }

    /// <summary>
    /// Optional account name used by the readiness health check for a cheap live probe.
    /// When unset, the health check reports healthy without contacting storage.
    /// </summary>
    public string? ProbeAccountName { get; set; }
}
