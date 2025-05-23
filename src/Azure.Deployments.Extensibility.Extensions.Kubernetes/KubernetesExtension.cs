// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Api;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation;
using Microsoft.AspNetCore.Http;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes
{
    internal class KubernetesExtension : IExtension
    {
        public const string ExtensionName = "Kubernetes";

        private readonly IK8sResourceSpecificationValidator resourceSpecificationValidator;
        private readonly IK8sResourceReferenceValidator resourceReferenceValidator;
        private readonly IK8sClientFactory k8sClientFactory;

        public KubernetesExtension(
            IK8sResourceSpecificationValidator resourceSpecificationValidator,
            IK8sResourceReferenceValidator resourceReferenceValidator,
            IK8sClientFactory k8sClientFactory)
        {
            this.resourceSpecificationValidator = resourceSpecificationValidator;
            this.resourceReferenceValidator = resourceReferenceValidator;
            this.k8sClientFactory = k8sClientFactory;
        }

        public async Task<IResult> PreviewResourceCreateOrUpdateAsync(HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
        {
            this.resourceSpecificationValidator.ValidateAndThrow(resourceSpecification);

            var groupVersionKind = ModelMapper.MapToGroupVersionKind(resourceSpecification.Type, resourceSpecification.ApiVersion);
            var k8sObject = new K8sObject(groupVersionKind, resourceSpecification.Properties);

            using var client = await this.k8sClientFactory.CreateAsync(resourceSpecification.Config);
            var api = await new K8sApiDiscovery(client).FindApiAsync(groupVersionKind, cancellationToken);

            var configId = CalculateConfigId(client);

            // Do config ID checksum validation if one is supplied. 
            if (resourceSpecification.ConfigId is not null && !TryMatchConfigurationId(resourceSpecification.ConfigId, configId, out var configIdErrorResult))
            {
                return configIdErrorResult;
            }
            
            if (api.Namespaced && (k8sObject.Namespace ?? client.DefaultNamespace) is { } @namespace)
            {
                if (await client.GetNamespaceAsync(@namespace, cancellationToken: cancellationToken) is null)
                {
                    // Namespace does not exist. Perform client-side dry run.
                    k8sObject.Metadata["namespace"] = @namespace;
                    
                    return Results.Ok(ModelMapper.MapToResource(K8sObjectIdentifiers.Create(k8sObject), k8sObject, configId));
                }
            }

            k8sObject = await api.PatchObjectAsync(k8sObject, dryRun: true, cancellationToken);

            return Results.Ok(ModelMapper.MapToResource(K8sObjectIdentifiers.Create(k8sObject), k8sObject, configId));
        }

        public async Task<IResult> CreateOrUpdateResourceAsync(HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
        {
            this.resourceSpecificationValidator.ValidateAndThrow(resourceSpecification);

            var groupVersionKind = ModelMapper.MapToGroupVersionKind(resourceSpecification.Type, resourceSpecification.ApiVersion);
            var k8sObject = new K8sObject(groupVersionKind, resourceSpecification.Properties);

            using var client = await this.k8sClientFactory.CreateAsync(resourceSpecification.Config);
            var api = await new K8sApiDiscovery(client).FindApiAsync(groupVersionKind, cancellationToken);

            // Do config ID checksum validation if one is supplied. 
            if (resourceSpecification.ConfigId is not null
                && (await api.GetObjectAsync(K8sObjectIdentifiers.Create(k8sObject), cancellationToken)) is not null
                && !TryMatchConfigurationId(resourceSpecification.ConfigId, CalculateConfigId(client), out var configIdErrorResult))
            {
                return configIdErrorResult;
            }

            k8sObject = await api.PatchObjectAsync(k8sObject, dryRun: false, cancellationToken);

            var configId = CalculateConfigId(client);

            return Results.Ok(ModelMapper.MapToResource(K8sObjectIdentifiers.Create(k8sObject), k8sObject, configId));
        }

        public async Task<IResult> GetResourceAsync(HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken)
        {
            this.resourceReferenceValidator.ValidateAndThrow(resourceReference);

            var groupVersionKind = ModelMapper.MapToGroupVersionKind(resourceReference.Type, resourceReference.ApiVersion);
            var identifiers = ModelMapper.MapToK8sObjectIdentifiers(resourceReference.Identifiers);
            
            using var client = await this.k8sClientFactory.CreateAsync(resourceReference.Config);

            // Do config ID checksum validation if one is supplied. 
            if (resourceReference.ConfigId is not null && !TryMatchConfigurationId(resourceReference.ConfigId, CalculateConfigId(client), out var configIdErrorResult))
            {
                return configIdErrorResult;
            }
            
            var api = await new K8sApiDiscovery(client).FindApiAsync(groupVersionKind, cancellationToken);

            if (await api.GetObjectAsync(identifiers, cancellationToken) is { } k8sObject)
            {
                var configId = CalculateConfigId(client);
                return Results.Ok(ModelMapper.MapToResource(identifiers, k8sObject, configId));
            }

            var @namespace = api.Namespaced ? identifiers.Namespace ?? client.DefaultNamespace : null;

            return Results.NotFound(new ErrorData
            {
                Error = new()
                {
                    Code = "ObjectNotFound",
                    Message = @namespace is null
                        ? $"The referenced Kubernetes object (GroupVersionKind={groupVersionKind}, Name={identifiers.Name}) was not found."
                        : $"The referenced Kubernetes object (GroupVersionKind={groupVersionKind}, Name={identifiers.Name}, Namespace={@namespace}) was not found.",
                },
            });
        }

        public async Task<IResult> DeleteResourceAsync(HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken)
        {
            this.resourceReferenceValidator.ValidateAndThrow(resourceReference);

            var identifiers = ModelMapper.MapToK8sObjectIdentifiers(resourceReference.Identifiers);

            // This can only be invoked by Deployment Stacks. ConfigId must be preserved
            // to ensure that the operation is performed on the correct cluster.
            if (resourceReference.ConfigId is null)
            {
                return Results.BadRequest(new ErrorData
                {
                    Error = new()
                    {
                        Code = "InvalidConfigId",
                        Message = "The resource reference is missing a config ID.",
                    },
                });
            }

            var groupVersionKind = ModelMapper.MapToGroupVersionKind(resourceReference.Type, resourceReference.ApiVersion);
            using var client = await this.k8sClientFactory.CreateAsync(resourceReference.Config);

            if (!TryMatchConfigurationId(resourceReference.ConfigId, CalculateConfigId(client), out var configIdErrorResult))
            {
                return configIdErrorResult;
            }

            var api = await new K8sApiDiscovery(client).FindApiAsync(groupVersionKind, cancellationToken);

            await api.DeleteObjectAsync(identifiers, cancellationToken);

            return Results.NoContent();
        }

        private static bool TryMatchConfigurationId(string userSentConfigId, string expectedConfigId, [NotNullWhen(false)] out IResult? errorResult)
        {
            errorResult = null;

            if (string.Equals(expectedConfigId, userSentConfigId, StringComparison.Ordinal))
            {
                return true;
            }

            errorResult = Results.UnprocessableEntity(
                new ErrorData
                {
                    Error = new()
                    {
                        Code = "ClusterMismatch",
                        Message = "The referenced Kubernetes object cannot be deleted because it may be deployed to a different cluster. Please verify that you are using the correct kubeconfig file and context.",
                    },
                });

            return false;
        }

        private static string CalculateConfigId(IK8sClient k8sClient) => K8sObjectIdentifiers.CalculateServerHostHash(k8sClient.ServerHost);
    }
}
