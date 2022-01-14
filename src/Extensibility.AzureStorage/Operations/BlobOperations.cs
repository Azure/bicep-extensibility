namespace Extensibility.AzureStorage.Operations
{
    using Azure.Storage.Blobs;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility.Core.Contract;
    using Extensibility.Core.Messages;

    internal class BlobOperations : IExtensibilityProvider
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

            var containerName = resource.Properties!["containerName"]!.ToString();
            var name = resource.Properties!["name"]!.ToString();
            var base64Content = resource.Properties!["base64Content"]!.ToString();

            var client = new BlobServiceClient(connectionString);

            var bytes = Convert.FromBase64String(base64Content);
            await client
                .GetBlobContainerClient(containerName)
                .GetBlobClient(name)
                .UploadAsync(new MemoryStream(bytes), overwrite: true, cancellationToken);

            return new()
            {
                Body = resource,
            };
        }
    }
}
