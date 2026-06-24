// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Storage;

/// <summary>
/// Caches one <see cref="BlobServiceClient"/> per account and authenticates every client with the
/// injected <see cref="TokenCredential"/> (locally this resolves to the developer's <c>az login</c>).
/// </summary>
public sealed class StorageClientFactory : IStorageClientFactory
{
    private const string DefaultEndpointSuffix = "blob.core.windows.net";

    private readonly TokenCredential credential;
    private readonly string endpointSuffix;
    private readonly ConcurrentDictionary<string, BlobServiceClient> serviceClients = new(StringComparer.Ordinal);

    public StorageClientFactory(TokenCredential credential, IOptions<BlobPocOptions> options)
    {
        this.credential = credential;
        this.endpointSuffix = string.IsNullOrWhiteSpace(options.Value.BlobEndpointSuffix)
            ? DefaultEndpointSuffix
            : options.Value.BlobEndpointSuffix;
    }

    public BlobServiceClient GetServiceClient(string accountName)
    {
        // Defense in depth: the handlers validate first, but never interpolate an unchecked
        // account name into the endpoint URL.
        if (StorageValidation.ValidateAccountName(accountName, "/properties/accountName") is not null)
        {
            throw new ArgumentException($"Invalid storage account name '{accountName}'.", nameof(accountName));
        }

        return this.serviceClients.GetOrAdd(
            accountName,
            name => new BlobServiceClient(new Uri($"https://{name}.{this.endpointSuffix}"), this.credential));
    }

    public BlobContainerClient GetContainerClient(string accountName, string containerName) =>
        this.GetServiceClient(accountName).GetBlobContainerClient(containerName);
}
