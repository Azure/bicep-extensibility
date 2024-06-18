// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.DependencyInjection;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration.TestFixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration
{
    public abstract class KubernetesExtensionTestBase
    {
        internal static KubernetesExtension Sut { get; } = CreateSut();

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

        private static KubernetesExtension CreateSut()
        {
            var serviceProvider = new ServiceCollection().AddKubernetesExtensionDispatcher().BuildServiceProvider();
            var extensionDispatcher = serviceProvider.GetRequiredKeyedService<IExtensionDispatcher>(KubernetesExtension.ExtensionName);

            return (KubernetesExtension)extensionDispatcher.DispatchExtension("1.0.0");
        }
    }
}
