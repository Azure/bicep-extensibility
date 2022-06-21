using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Json;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions;
using k8s;
using k8s.Autorest;
using System.Net;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes
{
    public class KubernetesProvider : IExtensibilityProvider
    {
        public const string ProviderName = "Kubernetes";

        public Task<ExtensibilityOperationResponse> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            HandleHttpOperationException(this.ProcessDeleteRequestAsync)(request, cancellationToken);

        public Task<ExtensibilityOperationResponse> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            HandleHttpOperationException(this.ProcessGetOperationAsync)(request, cancellationToken);

        public Task<ExtensibilityOperationResponse> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            HandleHttpOperationException(this.ProcessPreviewSaveRequestAsync)(request, cancellationToken);

        public Task<ExtensibilityOperationResponse> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            HandleHttpOperationException(this.ProcessSaveRequestAsync)(request, cancellationToken);

        private async Task<ExtensibilityOperationResponse> ProcessDeleteRequestAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var resource = await request.ProcessAsync(cancellationToken);

            await resource.DeleteAsync(cancellationToken);

            return new ExtensibilityOperationSuccessResponse(request.Resource);
        }

        private async Task<ExtensibilityOperationResponse> ProcessGetOperationAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var resource = await request.ProcessAsync(cancellationToken);

            var properties = await resource.GetAsync(cancellationToken);

            return new ExtensibilityOperationSuccessResponse(request.Resource with { Properties = properties });
        }

        private async Task<ExtensibilityOperationResponse> ProcessPreviewSaveRequestAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var resource = await request.ProcessAsync(cancellationToken);

            if (resource.Namespace is not null)
            {
                try
                {
                    await resource.Kubernetes.CoreV1.ReadNamespaceAsync(resource.Namespace, cancellationToken: cancellationToken);
                }
                catch (HttpOperationException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    // For namespaced resources we have to handle a special case where the namespace is being created as part of the same
                    // template. When we do the dry-run request this will fail if the namespace does not yet exist. This isn't useful to us
                    // because if the namespace is being created as part of the template this would be a false positive. So for these cases
                    // we want to fall back to a "client" dry-run if the namespace has yet to be created. This is lower fidelity but it won't 
                    // block things that would work.
                    var metadata = resource.Properties.Metadata with { Namespace = resource.Namespace };
                    var patchedProperties = resource.Properties with { Metadata = metadata };
                    var patchedPropertiesElement = JsonSerializers.CamelCase.SerializeToElement(patchedProperties);

                    return new ExtensibilityOperationSuccessResponse(request.Resource with { Properties = patchedPropertiesElement });
                }
            }

            var properties = await resource.PatchAsync(dryRun: "All", cancellationToken);

            return new ExtensibilityOperationSuccessResponse(request.Resource with { Properties = properties });
        }

        private async Task<ExtensibilityOperationResponse> ProcessSaveRequestAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var resource = await request.ProcessAsync(cancellationToken);

            var properties = await resource.PatchAsync(dryRun: null, cancellationToken);

            return new ExtensibilityOperationSuccessResponse(request.Resource with { Properties = properties });
        }

        private static ExtensibilityOperation HandleHttpOperationException(ExtensibilityOperation operation)
        {
            return async (ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await operation.Invoke(request, cancellationToken);
                }
                catch (HttpOperationException exception)
                {
                    throw exception.ToExtensibilityException();
                }
            };
        }
    }
}