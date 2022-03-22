namespace Extensibility.AzureStorage.Operations
{
    using Azure.Storage.Blobs;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility.Core.Contract;
    using Extensibility.Core.Messages;

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

            var staticWebsiteEnabled = resource.Properties!["staticWebsiteEnabled"]!.GetValue<bool>();
            var staticWebsiteIndexDocument = resource.Properties!["staticWebsiteIndexDocument"]!.GetValue<string>();
            var staticWebsiteErrorDocument404Path = resource.Properties!["staticWebsiteErrorDocument404Path"]!.GetValue<string>();

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
