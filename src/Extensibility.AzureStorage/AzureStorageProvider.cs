namespace Extensibility.AzureStorage
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility.AzureStorage.Operations;
    using Extensibility.Core.Contract;
    using Extensibility.Core.Messages;

    public class AzureStorageProvider : IExtensibilityProvider
    {
        private static readonly IReadOnlyDictionary<string, IExtensibilityProvider> OperationsLookup = new Dictionary<string, IExtensibilityProvider>
        {
            ["service"] = new ServiceOperations(),
            ["blob"] = new BlobOperations(),
            ["container"] = new ContainerOperations(),
        };

        public Task<DeleteResponse> Delete(DeleteRequest request, CancellationToken cancellationToken)
            => OperationsLookup[request.Body!.Type!].Delete(request, cancellationToken);

        public Task<GetResponse> Get(GetRequest request, CancellationToken cancellationToken)
            => OperationsLookup[request.Body!.Type!].Get(request, cancellationToken);

        public Task<PreviewSaveResponse> PreviewSave(PreviewSaveRequest request, CancellationToken cancellationToken)
            => OperationsLookup[request.Body!.Type!].PreviewSave(request, cancellationToken);

        public Task<SaveResponse> Save(SaveRequest request, CancellationToken cancellationToken)
            => OperationsLookup[request.Body!.Type!].Save(request, cancellationToken);
    }
}