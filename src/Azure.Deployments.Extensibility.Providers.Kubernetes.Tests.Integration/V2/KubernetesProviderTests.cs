// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.V2.AutoData;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Repositories;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;
using V2KubernetesProvider = Azure.Deployments.Extensibility.Providers.Kubernetes.V2.KubernetesProvider;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.V2
{
    /*
     * To run the tests locally:
     *   - Install Docker and minikube (https://minikube.sigs.k8s.io/docs/start/)
     *   - Run "minikube start"
     *
     * To clean up resources created by the tests:
     *   - Run "minikube delete"
     */
    public class KubernetesProviderTests
    {
        private const string ProviderVersion = "1.28.3";

        private static readonly HttpContext DummyHttpContext = new DefaultHttpContext();

        private static readonly V2KubernetesProvider Sut = new(
            new V1APIResourceCatalogServiceFactory(),
            new K8sResourceRepositoryFactory());

        [Theory, SampleNamespacedResourceRequestBodyAutoData]
        public async Task CreateResourceReferenceAsync_NamespacedResource_Ok(ResourceRequestBody request)
        {
            var result = await Sut.CreateResourceReferenceAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);

            result.Should().BeOfType<Ok<ResourceReferenceResponseBody>>()
                .Subject.Value.Should().BeOfType<ResourceReferenceResponseBody>()
                .Subject.ReferenceId.Should().StartWith("01-");
        }

        [Theory, SampleNamespacedResourceRequestBodyAutoData(@namespace: "non-existent")]
        public async Task PreviewResourceCreateOrUpdateAsync_NamespacedResourceWithNonExistentNamespace_Ok(ResourceRequestBody request)
        {
            var result = await Sut.PreviewResourceCreateOrUpdateAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);

            var resource = result.Should().BeOfType<Ok<ResourceResponseBody>>()
                .Subject.Value.Should().BeOfType<ResourceResponseBody>()
                .Subject;

            resource.ReferenceId.Should().StartWith("01-");
            resource.Properties["metadata"]?["uid"].Should().BeNull();
        }


        [Theory, SampleNamespacedResourceRequestBodyAutoData(@namespace: "default")]
        public async Task PreviewResourceCreateOrUpdateAsync_NamespacedResourceWithExistingNamespace_Ok(ResourceRequestBody request)
        {
            var result = await Sut.PreviewResourceCreateOrUpdateAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);

            var resource = result.Should().BeOfType<Ok<ResourceResponseBody>>()
                .Subject.Value.Should().BeOfType<ResourceResponseBody>()
                .Subject;

            resource.ReferenceId.Should().StartWith("01-");
            resource.Properties["metadata"]?["uid"].Should().NotBeNull();
        }

        [Theory, SampleNamespacedResourceRequestBodyAutoData]
        public async Task CreateOrUpdateResourceAsync_NamespacedResource_ReturnsOk(ResourceRequestBody request)
        {
            // Arrange.
            var createReferenceResult = (Ok<ResourceReferenceResponseBody>)await Sut.CreateResourceReferenceAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);
            var referenceId = createReferenceResult.Value!.ReferenceId;

            // Act.
            var result = await Sut.CreateOrUpdateResourceAsync(DummyHttpContext, ProviderVersion, referenceId, request, CancellationToken.None);

            // Asssert.
            var resource = result.Should().BeOfType<Ok<ResourceResponseBody>>()
                .Subject.Value.Should().BeOfType<ResourceResponseBody>()
                .Subject;

            resource.ReferenceId.Should().Be(referenceId);
            resource.Properties["metadata"]?["uid"].Should().NotBeNull();
        }

        [Theory, SampleNamespacedResourceRequestBodyAutoData]
        public async Task GetResourceByReferenceIdWithConfigAsync_NamespacedResource_Ok(ResourceRequestBody request)
        {
            // Arrange.
            var createReferenceResult = (Ok<ResourceReferenceResponseBody>)await Sut.CreateResourceReferenceAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);
            var referenceId = createReferenceResult.Value!.ReferenceId;

            await Sut.CreateOrUpdateResourceAsync(DummyHttpContext, ProviderVersion, referenceId, request, CancellationToken.None);

            // Act.
            var result = await Sut.GetResourceByReferenceIdWithConfigAsync(DummyHttpContext, ProviderVersion, referenceId, request.Config!, CancellationToken.None);

            // Assert.
            var resource = result.Should().BeOfType<Ok<ResourceResponseBody>>()
                .Subject.Value.Should().BeOfType<ResourceResponseBody>()
                .Subject;

            resource.ReferenceId.Should().Be(referenceId);
            resource.Properties["metadata"]?["uid"].Should().NotBeNull();
        }

        [Theory, SampleNamespacedResourceRequestBodyAutoData]
        public async Task DeleteResourceByReferenceIdWithConfigAsync_NamespacedResource_NoContent(ResourceRequestBody request)
        {
            // Arrange.
            var createReferenceResult = (Ok<ResourceReferenceResponseBody>)await Sut.CreateResourceReferenceAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);
            var referenceId = createReferenceResult.Value!.ReferenceId;

            await Sut.CreateOrUpdateResourceAsync(DummyHttpContext, ProviderVersion, referenceId, request, CancellationToken.None);
            await Task.Delay(2000);

            // Act.
            var result = await Sut.DeleteResourceByReferenceIdWithConfigAsync(DummyHttpContext, ProviderVersion, referenceId, request.Config!, CancellationToken.None);

            // Assert.
            result.Should().BeOfType<NoContent>();
        }

        [Theory, SampleClusterScopedResourceRequestBodyAutoData]
        public async Task CreateResourceReferenceAsync_ClusterScopedResource_Ok(ResourceRequestBody request)
        {
            var result = await Sut.CreateResourceReferenceAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);

            result.Should().BeOfType<Ok<ResourceReferenceResponseBody>>()
                .Subject.Value.Should().BeOfType<ResourceReferenceResponseBody>()
                .Subject.ReferenceId.Should().StartWith("01-");
        }

        [Theory, SampleClusterScopedResourceRequestBodyAutoData]
        public async Task PreviewResourceCreateOrUpdateAsync_ClusterScopedResource_Ok(ResourceRequestBody request)
        {
            var result = await Sut.PreviewResourceCreateOrUpdateAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);

            var resource = result.Should().BeOfType<Ok<ResourceResponseBody>>()
                .Subject.Value.Should().BeOfType<ResourceResponseBody>()
                .Subject;

            resource.ReferenceId.Should().StartWith("01-");
            resource.Properties["metadata"]?["uid"].Should().NotBeNull();
        }

        [Theory, SampleClusterScopedResourceRequestBodyAutoData]
        public async Task CreateOrUpdateResourceAsync_ClusterScopedResource_ReturnsOk(ResourceRequestBody request)
        {
            // Arrange.
            var createReferenceResult = (Ok<ResourceReferenceResponseBody>)await Sut.CreateResourceReferenceAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);
            var referenceId = createReferenceResult.Value!.ReferenceId;

            // Act.
            var result = await Sut.CreateOrUpdateResourceAsync(DummyHttpContext, ProviderVersion, referenceId, request, CancellationToken.None);

            // Asssert.
            var resource = result.Should().BeOfType<Ok<ResourceResponseBody>>()
                .Subject.Value.Should().BeOfType<ResourceResponseBody>()
                .Subject;

            resource.ReferenceId.Should().Be(referenceId);
            resource.Properties["metadata"]?["uid"].Should().NotBeNull();
        }

        [Theory, SampleClusterScopedResourceRequestBodyAutoData]
        public async Task GetResourceByReferenceIdWithConfigAsync_ClusterScopedResource_Ok(ResourceRequestBody request)
        {
            // Arrange.
            var createReferenceResult = (Ok<ResourceReferenceResponseBody>)await Sut.CreateResourceReferenceAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);
            var referenceId = createReferenceResult.Value!.ReferenceId;

            await Sut.CreateOrUpdateResourceAsync(DummyHttpContext, ProviderVersion, referenceId, request, CancellationToken.None);

            // Act.
            var result = await Sut.GetResourceByReferenceIdWithConfigAsync(DummyHttpContext, ProviderVersion, referenceId, request.Config!, CancellationToken.None);

            // Assert.
            var resource = result.Should().BeOfType<Ok<ResourceResponseBody>>()
                .Subject.Value.Should().BeOfType<ResourceResponseBody>()
                .Subject;

            resource.ReferenceId.Should().Be(referenceId);
            resource.Properties["metadata"]?["uid"].Should().NotBeNull();
        }

        [Theory, SampleClusterScopedResourceRequestBodyAutoData]
        public async Task DeleteResourceByReferenceIdWithConfigAsync_ClusterScopedResource_NoContent(ResourceRequestBody request)
        {
            // Arrange.
            var createReferenceResult = (Ok<ResourceReferenceResponseBody>)await Sut.CreateResourceReferenceAsync(DummyHttpContext, ProviderVersion, request, CancellationToken.None);
            var referenceId = createReferenceResult.Value!.ReferenceId;

            await Sut.CreateOrUpdateResourceAsync(DummyHttpContext, ProviderVersion, referenceId, request, CancellationToken.None);
            await Task.Delay(2000);

            // Act.
            var result = await Sut.DeleteResourceByReferenceIdWithConfigAsync(DummyHttpContext, ProviderVersion, referenceId, request.Config!, CancellationToken.None);

            // Assert.
            result.Should().BeOfType<NoContent>();
        }
    }
}
