// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Storage.Blobs;

namespace BlobPocExtension.Storage;

/// <summary>
/// Hands out Azure Storage clients authenticated with <c>DefaultAzureCredential</c>.
/// </summary>
public interface IStorageClientFactory
{
    BlobServiceClient GetServiceClient(string accountName);

    BlobContainerClient GetContainerClient(string accountName, string containerName);

    // BlobClient GetBlobClient(string accountName, string containerName, string blobName);
    //   ^ added when the Blob resource type is promoted (see implementation guide §8.10).
}
