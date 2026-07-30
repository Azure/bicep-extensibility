// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Storage.Blobs.Models;

namespace BlobPocExtension;

public record ContainerProperties
{
    public required string AccountName { get; init; }
    public required string ContainerName { get; init; }
    public string? PublicAccess { get; init; }   // "none" (default), "blob", or "container"
}

public record ContainerIdentifiers
{
    public required string AccountName { get; init; }
    public required string ContainerName { get; init; }
}

/// <summary>
/// Maps between the wire-level <c>publicAccess</c> string and the Azure SDK
/// <see cref="PublicAccessType"/> enum.
/// </summary>
internal static class ContainerAccess
{
    public static PublicAccessType ToPublicAccessType(string? value) => value?.ToLowerInvariant() switch
    {
        "blob" => PublicAccessType.Blob,
        "container" => PublicAccessType.BlobContainer,
        _ => PublicAccessType.None,
    };

    public static string FromPublicAccessType(PublicAccessType? value) => value switch
    {
        PublicAccessType.Blob => "blob",
        PublicAccessType.BlobContainer => "container",
        _ => "none",
    };
}
