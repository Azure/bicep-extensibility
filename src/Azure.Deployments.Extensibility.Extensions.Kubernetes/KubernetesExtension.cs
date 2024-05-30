// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Api;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using Microsoft.AspNetCore.Http;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes
{
    internal class KubernetesExtension : IExtension
    {
        private readonly IModelValidator<ResourceSpecification> resourceSpecificationValidator;
        private readonly IModelValidator<ResourceReference> resourceReferenceValidator;
        private readonly IK8sClientFactory k8sClientFactory;

        public KubernetesExtension(
            IModelValidator<ResourceSpecification> resourceSpecificationValidator,
            IModelValidator<ResourceReference> resourceReferenceValidator,
            IK8sClientFactory k8sClientFactory)
        {
            this.resourceSpecificationValidator = resourceSpecificationValidator;
            this.resourceReferenceValidator = resourceReferenceValidator;
            this.k8sClientFactory = k8sClientFactory;
        }

        public Task<IResult> PreviewResourceCreateOrUpdateAsync(HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken) =>
            HandleErrorResponseException(async () =>
            {
                this.resourceSpecificationValidator.ValidateAndThrow(resourceSpecification);

                var groupVersionKind = ModelMapper.MapToGroupVersionKind(resourceSpecification.Type, resourceSpecification.ApiVersion);
                var client = await this.k8sClientFactory.CreateAsync(resourceSpecification.Config);
                var api = await new K8sApiDiscovery(client).FindApiAsync(groupVersionKind, cancellationToken);
                var k8sObject = new K8sObject(groupVersionKind, resourceSpecification.Properties);

                if (api.Namespaced && (k8sObject.Namespace ?? client.DefaultNamespace) is { } @namespace)
                {
                    if (await client.GetNamespaceAsync(@namespace, cancellationToken: cancellationToken) is null)
                    {
                        // Namespace does not exist. Perform client-side dry run.
                        k8sObject.Metadata["namespace"] = @namespace;

                        return Results.Ok(ModelMapper.MapToResource(K8sObjectIdentifiers.Create(k8sObject, client.ServerHost), k8sObject));
                    }
                }

                k8sObject = await api.PatchObjectAsync(k8sObject, dryRun: true, cancellationToken);

                return Results.Ok(ModelMapper.MapToResource(K8sObjectIdentifiers.Create(k8sObject, client.ServerHost), k8sObject));
            });

        public Task<IResult> CreateOrUpdateResourceAsync(HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken) =>
            HandleErrorResponseException(async () =>
            {
                this.resourceSpecificationValidator.ValidateAndThrow(resourceSpecification);

                var groupVersionKind = ModelMapper.MapToGroupVersionKind(resourceSpecification.Type, resourceSpecification.ApiVersion);
                var client = await this.k8sClientFactory.CreateAsync(resourceSpecification.Config);
                var api = await new K8sApiDiscovery(client).FindApiAsync(groupVersionKind, cancellationToken);
                var k8sObject = new K8sObject(groupVersionKind, resourceSpecification.Properties);

                k8sObject = await api.PatchObjectAsync(k8sObject, dryRun: false, cancellationToken);

                return Results.Ok(ModelMapper.MapToResource(K8sObjectIdentifiers.Create(k8sObject, client.ServerHost), k8sObject));
            });

        public Task<IResult> GetResourceAsync(HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken) =>
            HandleErrorResponseException(async () =>
            {
                this.resourceReferenceValidator.ValidateAndThrow(resourceReference);

                var groupVersionKind = ModelMapper.MapToGroupVersionKind(resourceReference.Type, resourceReference.ApiVersion);
                var client = await this.k8sClientFactory.CreateAsync(resourceReference.Config);
                var api = await new K8sApiDiscovery(client).FindApiAsync(groupVersionKind, cancellationToken);
                var identifiers = K8sObjectIdentifiers.From(resourceReference.Identifiers);

                if (await api.GetObjectAsync(identifiers, cancellationToken) is { } k8sObject)
                {
                    return Results.Ok(ModelMapper.MapToResource(identifiers, k8sObject));
                }

                return Results.NotFound(new ErrorData
                {
                    Error = new()
                    {
                        Code = "ObjectNotFound",
                        Message = "The referenced Kubernetes object was not found.",
                    },
                });
            });

        public Task<IResult> DeleteResourceAsync(HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken) =>
            HandleErrorResponseException(async () =>
            {
                this.resourceReferenceValidator.ValidateAndThrow(resourceReference);

                var identifiers = K8sObjectIdentifiers.From(resourceReference.Identifiers);

                // This can only be invoked by Deployment Stacks. ServerHostHash must be preserved
                // to ensure that the operation is performed on the correct cluster.
                if (identifiers.ServerHostHash is null)
                {
                    return Results.BadRequest(new ErrorData
                    {
                        Error = new()
                        {
                            Code = "InvalidResourceIdentifiers",
                            Message = "The resource identifiers is missing the server host hash.",
                        },
                    });
                }

                var groupVersionKind = ModelMapper.MapToGroupVersionKind(resourceReference.Type, resourceReference.ApiVersion);
                var client = await this.k8sClientFactory.CreateAsync(resourceReference.Config);

                if (!identifiers.MatchesServerHost(client.ServerHost))
                {
                    return Results.UnprocessableEntity(new ErrorData
                    {
                        Error = new()
                        {
                            Code = "ClusterMismatch",
                            Message = "The referenced Kubernetes object cannot be deleted because it is deployed to a different cluster. Please verify that you are using the correct kubeConfig file and context.",
                        },
                    });
                }

                var api = await new K8sApiDiscovery(client).FindApiAsync(groupVersionKind, cancellationToken);

                await api.DeleteObjectAsync(identifiers, cancellationToken);

                return Results.NoContent();
            });

        private static async Task<IResult> HandleErrorResponseException(Func<Task<IResult>> opeartion)
        {
            try
            {
                return await opeartion();
            }
            catch (ErrorResponseException exception)
            {
                return Results.BadRequest(exception.ToErrorData());
            }
        }
    }
}
