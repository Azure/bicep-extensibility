// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration.TestFixtures;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation;
using Microsoft.AspNetCore.Http;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration
{
    public abstract class KubernetesExtensionTestBase
    {
        internal static KubernetesExtension Sut { get; } = new(new ResourceSpecificationValidator(), new ResourceReferenceValidator(), new K8sClientFactory());

        internal static HttpContext DefaultHttpContext { get; } = new DefaultHttpContext();

        protected static Task<IResult> PreviewResourceCreateOrUpdateAsync(ResourceSpecification specification) =>
            Sut.PreviewResourceCreateOrUpdateAsync(DefaultHttpContext, specification, default);

        protected static Task<IResult> CreateOrUpdateResourceAsync(ResourceSpecification specification) =>
            Sut.CreateOrUpdateResourceAsync(DefaultHttpContext, specification, default);

        protected static Task<IResult> GetResourceAsync(ResourceReference reference) =>
            Sut.GetResourceAsync(DefaultHttpContext, reference, default);

        protected static Task<IResult> DeleteResourceAsync(ResourceReference reference) =>
            Sut.DeleteResourceAsync(DefaultHttpContext, reference, default);

        protected static Task<IResult> CreateOrUpdateNamespaceAsync(string? namespaceName)
        {
            ArgumentNullException.ThrowIfNull(namespaceName);

            var specification = new K8sNamespaceSpecification(namespaceName);

            return CreateOrUpdateNamespaceAsync(specification);
        }

        protected static Task<IResult> CreateOrUpdateNamespaceAsync(K8sNamespaceSpecification specification) => CreateOrUpdateResourceAsync(specification);
    }
}
