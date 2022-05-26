using Azure.ResourceManager.Extensibility.Core;
using Azure.ResourceManager.Extensibility.Core.Exceptions;

namespace Azure.ResourceManager.Extensibility.Registries
{
    public class FirstPartyExtensibilityProviderProxy : IExtensibilityProvider
    {
        private readonly IExtensibilityProvider provider;

        public FirstPartyExtensibilityProviderProxy(IExtensibilityProvider provider)
        {
            this.provider = provider;
        }

        public Task<ExtensibilityOperationResponse> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            HandleExceptions(this.provider.DeleteAsync)(request, cancellationToken);

        public Task<ExtensibilityOperationResponse> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            HandleExceptions(this.provider.GetAsync)(request, cancellationToken);

        public Task<ExtensibilityOperationResponse> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            HandleExceptions(this.provider.PreviewSaveAsync)(request, cancellationToken);

        public Task<ExtensibilityOperationResponse> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            HandleExceptions(this.provider.SaveAsync)(request, cancellationToken);
        

        private static ExtensibilityOperation HandleExceptions(ExtensibilityOperation operation)
        {
            return async (ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await operation.Invoke(request, cancellationToken);
                }
                catch (ExtensibilityException extensibilityException)
                {
                    return new ExtensibilityOperationErrorResponse(extensibilityException.Errors);
                }
            };
        }
    }
}
