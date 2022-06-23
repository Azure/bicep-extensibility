// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Attributes;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Customizations;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Mocks;
using FluentAssertions;
using Json.More;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Xunit;
using Xunit.Abstractions;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit
{
    public class KubernetesProviderTests
    {
        private readonly ITestOutputHelper testOutput;

        public KubernetesProviderTests(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

        [Theory, NamespacedRequestAutoData]
        public async Task DeleteAsync_NamespacedRequest_Succeeds(Fixture fixture, ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var apiResourceList = fixture
                .Customize(new SampleNamespacedApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(
                this.testOutput,
                httpContext => httpContext.Response.WriteAsJsonAsync(apiResourceList),
                httpContext => httpContext.Response.WriteAsJsonAsync(request.Resource.Properties));

            request = server.InjectKubeConfig(request);

            var response = await sut.DeleteAsync(request, CancellationToken.None);

            response.Errors.Should().BeNull();
            response.Resource.Should().Be(request.Resource);
        }

        [Theory, ClusterRequestAutoData]
        public async Task DeleteAsync_ClusterRequest_Succeeds(Fixture fixture, ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var apiResourceList = fixture
                .Customize(new SampleClusterApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(
                this.testOutput,
                httpContext => httpContext.Response.WriteAsJsonAsync(apiResourceList),
                httpContext => httpContext.Response.WriteAsJsonAsync(request.Resource.Properties));

            request = server.InjectKubeConfig(request);

            var response = await sut.DeleteAsync(request, CancellationToken.None);

            response.Errors.Should().BeNull();
            response.Resource.Should().Be(request.Resource);
        }

        [Theory, NamespacedRequestAutoData]
        public async Task GetAsync_NamespacedRequest_Succeeds(Fixture fixture, ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var apiResourceList = fixture
                .Customize(new SampleNamespacedApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(
                this.testOutput,
                httpContext => httpContext.Response.WriteAsJsonAsync(apiResourceList),
                httpContext => httpContext.Response.WriteAsync("{}"));

            request = server.InjectKubeConfig(request);

            var response = await sut.GetAsync(request, CancellationToken.None);

            response.Errors.Should().BeNull();
            response.Resource.Should().NotBeNull();
            response.Resource!.Properties.ToString().Should().Be("{}");
        }

        [Theory, ClusterRequestAutoData]
        public async Task GetAsync_ClusterRequest_Succeeds(Fixture fixture, ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var apiResourceList = fixture
                .Customize(new SampleClusterApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(
                this.testOutput,
                httpContext => httpContext.Response.WriteAsJsonAsync(apiResourceList),
                httpContext => httpContext.Response.WriteAsync("{}"));

            request = server.InjectKubeConfig(request);

            var response = await sut.GetAsync(request, CancellationToken.None);

            response.Errors.Should().BeNull();
            response.Resource.Should().NotBeNull();
            response.Resource!.Properties.ToString().Should().Be("{}");
        }

        [Theory, ClusterRequestAutoData]
        public async Task PreviewSaveAsync_NamespaceNotFound_DoesClientSideDryRun(Fixture fixture, ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var apiResourceList = fixture
                .Customize(new SampleNamespacedApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(
                this.testOutput,
                httpContext => httpContext.Response.WriteAsJsonAsync(apiResourceList),
                httpContext => Task.Run(() => httpContext.Response.StatusCode = 404));

            request = server.InjectKubeConfig(request);

            var response = await sut.PreviewSaveAsync(request, CancellationToken.None);

            response.Errors.Should().BeNull();
            response.Resource.Should().NotBeNull();

            var resource = ModelMapper.MapToConcrete<KubernetesResourceProperties>(response.Resource!);
            var import = ModelMapper.MapToConcrete<KubernetesConfig>(request.Import);

            // PreviewSave should patch the namespace property.
            resource.Properties.Metadata.Namespace.Should().Be(import.Config.Namespace);
        }

        [Theory, NamespacedRequestAutoData]
        public async Task PreviewSaveAsync_NamespacedRequest_Succeeds(Fixture fixture, ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var apiResourceList = fixture
                .Customize(new SampleNamespacedApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(
                this.testOutput,
                httpContext => httpContext.Response.WriteAsJsonAsync(apiResourceList),
                httpContext => httpContext.Response.WriteAsync("{}"),
                HttpContext => HttpContext.Response.WriteAsJsonAsync(true));

            request = server.InjectKubeConfig(request);

            var response = await sut.PreviewSaveAsync(request, CancellationToken.None);

            response.Errors.Should().BeNull();
            response.Resource.Should().NotBeNull();
            response.Resource!.Properties.GetBoolean().Should().BeTrue();
        }

        [Theory, ClusterRequestAutoData]
        public async Task PreviewSaveAsync_ClusterRequest_Succeeds(Fixture fixture, ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var apiResourceList = fixture
                .Customize(new SampleClusterApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(
                this.testOutput,
                httpContext => httpContext.Response.WriteAsJsonAsync(apiResourceList),
                HttpContext => HttpContext.Response.WriteAsJsonAsync(true));

            request = server.InjectKubeConfig(request);

            var response = await sut.PreviewSaveAsync(request, CancellationToken.None);

            response.Errors.Should().BeNull();
            response.Resource.Should().NotBeNull();
            response.Resource!.Properties.GetBoolean().Should().BeTrue();
        }


        [Theory, NamespacedRequestAutoData]
        public async Task SaveAsync_NamespacedRequest_Succeeds(Fixture fixture, ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var apiResourceList = fixture
                .Customize(new SampleNamespacedApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(
                this.testOutput,
                httpContext => httpContext.Response.WriteAsJsonAsync(apiResourceList),
                httpContext => httpContext.Response.WriteAsync("{}"));

            request = server.InjectKubeConfig(request);

            var response = await sut.SaveAsync(request, CancellationToken.None);

            response.Errors.Should().BeNull();
            response.Resource.Should().NotBeNull();
            response.Resource!.Properties.ToString().Should().Be("{}");
        }

        [Theory, ClusterRequestAutoData]
        public async Task SaveAsync_ClusterRequest_Succeeds(Fixture fixture, ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var apiResourceList = fixture
                .Customize(new SampleClusterApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(
                this.testOutput,
                httpContext => httpContext.Response.WriteAsJsonAsync(apiResourceList),
                httpContext => httpContext.Response.WriteAsync("{}"));

            request = server.InjectKubeConfig(request);

            var response = await sut.SaveAsync(request, CancellationToken.None);

            response.Errors.Should().BeNull();
            response.Resource.Should().NotBeNull();
            response.Resource!.Properties.ToString().Should().Be("{}");
        }
    }
}
