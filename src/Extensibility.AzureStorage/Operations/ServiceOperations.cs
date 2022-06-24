// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Storage.Blobs;
using Extensibility.Core.Contract;
using Extensibility.Core.Messages;

namespace Extensibility.AzureStorage.Operations
{
    internal class ServiceOperations : IExtensibilityProvider
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

            var staticWebsiteEnabled = resource.Properties!["staticWebsiteEnabled"].ToObject<bool>();
            var staticWebsiteIndexDocument = resource.Properties!["staticWebsiteIndexDocument"].ToObject<string>();
            var staticWebsiteErrorDocument404Path = resource.Properties!["staticWebsiteErrorDocument404Path"].ToObject<string>();

            var client = new BlobServiceClient(connectionString);
            await client.SetPropertiesAsync(new Azure.Storage.Blobs.Models.BlobServiceProperties
            {
                StaticWebsite = new Azure.Storage.Blobs.Models.BlobStaticWebsite
                {
                    Enabled = staticWebsiteEnabled,
                    IndexDocument = staticWebsiteIndexDocument,
                    ErrorDocument404Path = staticWebsiteErrorDocument404Path,
                },
            }, cancellationToken);

            return new()
            {
                Body = resource,
            };
        }
    }
}
