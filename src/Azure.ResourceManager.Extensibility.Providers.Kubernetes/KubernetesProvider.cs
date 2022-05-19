using Azure.ResourceManager.Extensibility.Core;
using FluentValidation;

namespace Azure.ResourceManager.Extensibility.Providers.Kubernetes
{
    public class KubernetesProvider : IExtensibilityProvider
    {
        public const string ProviderName = "Kubernetes";

        public Task<ExtensibilityResponse> DeleteAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensibilityResponse> GetAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensibilityResponse> PreviewSaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensibilityResponse> SaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}