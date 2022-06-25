// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Storage.Blobs;
using Extensibility.Core.Contract;
using Extensibility.Core.Messages;

namespace Extensibility.AzureStorage.Operations
{
    internal class ContainerOperations : IExtensibilityProvider
    {
        public Task<DeleteResponse> Delete(DeleteRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<GetResponse> Get(GetRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<PreviewSaveResponse> PreviewSave(PreviewSaveRequest request, CancellationToken cancellationToken)
        {
            await Task.Yield();

            return new()
            {
                Body = request.Body,
            };
        }

        public async Task<SaveResponse> Save(SaveRequest request, CancellationToken cancellationToken)
        {
            var resource = request.Body!;
            var connectionString = resource.Import!.Config!["connectionString"]!.ToString();

            var containerName = resource.Properties!["name"]!.ToString();

            var client = new BlobServiceClient(connectionString);

            await client
                .GetBlobContainerClient(containerName)
                .CreateIfNotExistsAsync();

            return new()
            {
                Body = resource,
            };
        }
    }
}
