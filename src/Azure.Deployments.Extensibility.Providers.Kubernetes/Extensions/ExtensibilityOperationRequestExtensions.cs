// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Azure.Deployments.Extensibility.Core.Extensions;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using k8s;
using k8s.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions
{
    public static class ExtensibilityOperationRequestExtensions
    {
        public async static Task<KubernetesResource> ProcessAsync(this ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var (import, resource) = Validate(request);
            var resourceType = KubernetesResourceType.Parse(resource.Type);

            var properties = resource.Properties
                .PatchProperty("apiVersion", resourceType.ApiVersion)
                .PatchProperty("kind", resourceType.Kind);

            var clientConfiguration = await CreateClientConfigurationAsync(import);

            IKubernetes? kubernetes = null;

            try
            {
                kubernetes = new k8s.Kubernetes(clientConfiguration);
                var client = new GenericClient(kubernetes, resourceType.Group, resourceType.Version, plural: "");

                var apiResouceList = await client.ListAsync<V1APIResourceList>(cancellationToken);
                var apiResource = apiResouceList.Resources.FirstOrDefault(x => x.Kind.Equals(resourceType.Kind, StringComparison.Ordinal));

                if (apiResource is null)
                {
                    throw new ExtensibilityException(
                        "UnknownResourceKind",
                        resource.GetJsonPointer(x => x.Type),
                        @$"Unknown resource kind ""{resourceType.Kind}"" in resource type ""{resource.Type}"".");
                }

                if (!apiResource.Namespaced && properties.Metadata.Namespace is not null)
                {
                    throw new ExtensibilityException(
                        "NamespaceSpecifiedForClusterResource",
                        resource.GetJsonPointer(x => x.Properties.Metadata.Namespace!),
                        "A namespace should not be specified for a cluster-scoped resource.");
                }

                var @namespace = apiResource.Namespaced
                    ? resource.Properties.Metadata.Namespace ?? import.Config.Namespace
                    : null;

                return new(kubernetes, resourceType.Group, resourceType.Version, @namespace, apiResource.Name, properties);
            }
            catch
            {
                kubernetes?.Dispose();

                throw;
            }
        }

        private static ExtensibilityOperationRequest<KubernetesConfig, KubernetesResourceProperties> Validate(ExtensibilityOperationRequest request)
        {
            // Validate kubeConfig format.
            if (request.Import.Config.TryGetProperty("kubeConfig", out var kubeConfig) && !kubeConfig.GetString().IsBase64Encoded())
            {
                throw new ExtensibilityException(
                    "InvalidKubeConfig",
                    request.Import.GetJsonPointer(x => x.Config).Combine("kubeConfig"),
                    @$"Value must be a Base64-encoded string.");
            }

            // Run JSON schema validation.
            return request.Validate<KubernetesConfig, KubernetesResourceProperties>(
                KubernetesConfig.Schema,
                KubernetesResourceType.TypePattern,
                KubernetesResourceProperties.Schema);
        }

        private static async Task<KubernetesClientConfiguration> CreateClientConfigurationAsync(ExtensibleImport<KubernetesConfig> import)
        {
            try
            {
                return await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(
                    new MemoryStream(import.Config.KubeConfig),
                    currentContext: import.Config.Context);
            }
            catch (Exception exception)
            {
                exception = exception.GetBaseException();

                throw new ExtensibilityException(
                    "InvalidImportConfig",
                    import.GetJsonPointer(x => x.Config),
                    exception.Message ?? exception.ToString());
            }
        }
    }
}
