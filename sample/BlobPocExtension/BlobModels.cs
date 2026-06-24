// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace BlobPocExtension;

public record BlobProperties
{
    public required string AccountName { get; init; }
    public required string ContainerName { get; init; }
    public required string BlobName { get; init; }
    public string? Content { get; init; }      // write on createOrUpdate; populated on get
    public string? ContentType { get; init; }
}

public record BlobIdentifiers
{
    public required string AccountName { get; init; }
    public required string ContainerName { get; init; }
    public required string BlobName { get; init; }
}
