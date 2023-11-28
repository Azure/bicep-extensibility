// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2;
using Azure.Deployments.Extensibility.Core.V2.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validators;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Repositories;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Schemas;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Validators;
using Json.Pointer;
using k8s;
using k8s.Autorest;
using k8s.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2
{
    public class KubernetesProvider(
        IV1APIResourceCatalogServiceFactory v1APIResourceCatalogServiceFactory,
        IK8sResourceRepositoryFactory k8sResourceRepositoryFactory) : IExtensibilityProvider
    {
        public const string ProviderName = "Kubernetes";

        private static readonly K8sClusterAccessConfigValidator KubernetesClusterAccessConfigValidator = new();

        private static readonly ResourceRequestBodyValidator ResourceRequestBodyValidator = new(
            new ResourceTypeRegexValidator(K8sResourceType.Pattern()),
            new ResourcePropertiesSchemaValidator(JsonSchemas.K8sResourceProperties),
            KubernetesClusterAccessConfigValidator);

        public async Task<IResult> CreateResourceReferenceAsync(HttpContext httpContext, string providerVersion, ResourceRequestBody requestBody, CancellationToken cancellationToken)
        {
            try
            {
                ResourceRequestBodyValidator.ValidateAndThrow(requestBody);

                var config = await K8sClusterAccessConfig.FromAsync(requestBody.Config);
                var resourceType = K8sResourceType.Parse(requestBody.Type);

                using var kubernetes = new k8s.Kubernetes(config.ClientConfiguration);
                var referenceId = await this.CreateResourceReferenceIdAsync(kubernetes, providerVersion, resourceType, config, requestBody, cancellationToken);

                return Results.Ok(new ResourceReferenceResponseBody(referenceId));
            }
            catch (KubeConfigException kubeConfigException)
            {
                return InvalidKubeConfig(kubeConfigException.Message);
            }
            catch (HttpOperationException httpOperationException) when ( httpOperationException.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                return KubernetesOperationError(httpOperationException.Message) ;
            }
        }

        public async Task<IResult> PreviewResourceCreateOrUpdateAsync(HttpContext httpContext, string providerVersion, ResourceRequestBody requestBody, CancellationToken cancellationToken)
        {
            try
            {
                ResourceRequestBodyValidator.ValidateAndThrow(requestBody);

                var config = await K8sClusterAccessConfig.FromAsync(requestBody.Config);
                var resourceType = K8sResourceType.Parse(requestBody.Type);

                using var kubernetes = new k8s.Kubernetes(config.ClientConfiguration);
                var referenceId = await this.CreateResourceReferenceIdAsync(kubernetes, providerVersion, resourceType, config, requestBody, cancellationToken);

                if (referenceId.Namespace is { } @namespace)
                {
                    try
                    {
                        await kubernetes.CoreV1.ReadNamespaceAsync(@namespace, cancellationToken: cancellationToken);
                    }
                    catch (HttpOperationException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
                    {
                        // Namespace does not exist. Perform client-side dry run.
                        return Results.Ok(new ResourceResponseBody(requestBody.Type, requestBody.Properties, referenceId));
                    }
                }

                var resource = new K8sResource(referenceId, resourceType, requestBody.Properties);
                var repository = k8sResourceRepositoryFactory.CreateK8sResourceRepository(kubernetes, resource.Namespace);

                resource = await repository.SaveAsync(resource, dryRun: true, cancellationToken);

                return Results.Ok(new ResourceResponseBody(requestBody.Type, resource.Properties, referenceId));
            }
            catch (KubeConfigException kubeConfigException)
            {
                throw new ErrorResponseException("InvalidConfig", kubeConfigException.Message, JsonPointer.Create("config", "kubeConfig"));
            }
            catch (HttpOperationException httpOperationException)
            {
                return KubernetesOperationError(httpOperationException.Message) ;
            }
        }

        public async Task<IResult>CreateOrUpdateResourceAsync(HttpContext httpContext, string providerVersion, string referenceId, ResourceRequestBody requestBody, CancellationToken cancellationToken)
        {
            try
            {
                ResourceRequestBodyValidator.ValidateAndThrow(requestBody);

                var resourceType = K8sResourceType.Parse(requestBody.Type);
                var resource = new K8sResource(referenceId, resourceType, requestBody.Properties);

                using var kubernetes = await BuildKubernetesClientAsync(requestBody.Config);
                var repository = k8sResourceRepositoryFactory.CreateK8sResourceRepository(kubernetes, resource.Namespace);

                resource = await repository.SaveAsync(resource, dryRun: false, cancellationToken);

                return Results.Ok(new ResourceResponseBody(requestBody.Type, resource.Properties, referenceId));
            }
            catch (HttpOperationException httpOperationException)
            {
                return KubernetesOperationError(httpOperationException.Message) ;
            }
        }

        public async Task<IResult> GetResourceByReferenceIdWithConfigAsync(HttpContext httpContext, string providerVersion, string referenceId, JsonObject configObject, CancellationToken cancellationToken)
        {
            K8sResourceReferenceId typedReferenceId = referenceId;
            using var kubernetes = await BuildKubernetesClientAsync(configObject);
            var repository = k8sResourceRepositoryFactory.CreateK8sResourceRepository(kubernetes, typedReferenceId.Namespace);

            var resource = await repository.TryGetByReferenceIdAsync(typedReferenceId, cancellationToken);

            return resource is not null
                ? Results.Ok(new ResourceResponseBody(resource.Type, resource.Properties, referenceId))
                : Results.NotFound();
        }

        public async Task<IResult> DeleteResourceByReferenceIdWithConfigAsync(HttpContext httpContext, string providerVersion, string referenceId, JsonObject configObject, CancellationToken cancellationToken)
        {
            K8sResourceReferenceId typedReferenceId = referenceId;
            using var kubernetes = await BuildKubernetesClientAsync(configObject);
            var repository = k8sResourceRepositoryFactory.CreateK8sResourceRepository(kubernetes, typedReferenceId.Namespace);

            await repository.DeleteByReferenceIdAsync(typedReferenceId, cancellationToken);

            return Results.NoContent();
        }

        public Task<IResult> GetResourceByReferenceIdAsync(HttpContext httpContext, string providerVersion, string referenceId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IResult> DeleteResourceByReferenceIdAsync(HttpContext httpContext, string providerVersion, string referenceId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IResult> GetResourceOperationByOperationIdAsync(HttpContext httpContext, string providerVersion, string operationId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static IResult KubernetesOperationError(string errorMessage)
        {
            var error = new Error("KubernetesOperationFailure", errorMessage, JsonPointer.Create("config", "kubeConfig"));

            return Results.BadRequest(new ErrorResponseBody(error));
        }

        private static IResult InvalidKubeConfig(string errorMessage)
        {
            var error = new Error("InvalidKubeConfig", errorMessage, JsonPointer.Create("config", "kubeConfig"));

            return Results.BadRequest(new ErrorResponseBody(error));
        }

        private static async Task<IKubernetes> BuildKubernetesClientAsync(JsonObject? configObject)
        {
            var config = await K8sClusterAccessConfig.FromAsync(configObject);

            return new k8s.Kubernetes(config.ClientConfiguration);
        }

        private async Task<K8sResourceReferenceId> CreateResourceReferenceIdAsync(IKubernetes kubernetes, string providerVersion, K8sResourceType resourceType, K8sClusterAccessConfig config, ResourceRequestBody requestBody, CancellationToken cancellationToken)
        {
            var apiResourceCatalogService = v1APIResourceCatalogServiceFactory.CreateV1APIResourceCatalogService(kubernetes);
            var apiResource = await apiResourceCatalogService.FindV1APIResourceAsync(providerVersion, resourceType, cancellationToken);

            return K8sResourceReferenceId.Create(apiResource, config, requestBody);
        }
    }
}
