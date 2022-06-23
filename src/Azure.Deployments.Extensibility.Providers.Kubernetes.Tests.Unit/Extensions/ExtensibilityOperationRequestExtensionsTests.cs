// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Attributes;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Customizations;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Mocks;
using FluentAssertions;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Xunit;
using Xunit.Abstractions;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Extensions
{
    public class ExtensibilityOperationRequestExtensionsTests
    {
        private readonly ITestOutputHelper testOutput;

        public ExtensibilityOperationRequestExtensionsTests(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

        [Theory, ClusterRequestAutoData]
        public async Task ProcessAsync_UnknownResourceKind_ThrowsException(V1APIResourceList apiResourcesWithArbitraryKind, ExtensibilityOperationRequest request)
        {
            await using var server = await MockKubernetesApiServer.StartAsync(this.testOutput, httpContext =>
                httpContext.Response.WriteAsJsonAsync(apiResourcesWithArbitraryKind));

            var sut = server.InjectKubeConfig(request);

            var assertion = await FluentActions.Invoking(async () => await sut.ProcessAsync(CancellationToken.None))
                .Should()
                .ThrowAsync<ExtensibilityException>();

            var errors = assertion.Which.Errors.ToArray();

            errors.Should().HaveCount(1);
            errors[0].Code.Should().Be("UnknownResourceKind");
            errors[0].Target.ToString().Should().Be($"/resources/{request.Resource.SymbolicName}/type");
            errors[0].Message.Should().Be(@"Unknown resource kind ""sampleKind"" in resource type ""sampleGroup/sampleKind@v1"".");
        }

        [Theory, NamespacedRequestAutoData]
        public async Task ProcessAsync_NamespaceSpecifiedForClusterResource_ThrowsException(Fixture fixture, ExtensibilityOperationRequest request)
        {
            var apiResourceList = fixture
                .Customize(new SampleClusterApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(this.testOutput, httpContext =>
                httpContext.Response.WriteAsJsonAsync(apiResourceList));

            var sut = server.InjectKubeConfig(request);

            var assertion = await FluentActions.Invoking(async () => await sut.ProcessAsync(CancellationToken.None))
                .Should()
                .ThrowAsync<ExtensibilityException>();

            var errors = assertion.Which.Errors.ToArray();

            errors.Should().HaveCount(1);
            errors[0].Code.Should().Be("NamespaceSpecifiedForClusterResource");
            errors[0].Target.ToString().Should().Be($"/resources/{request.Resource.SymbolicName}/properties/metadata/namespace");
            errors[0].Message.Should().Be("A namespace should not be specified for a cluster-scoped resource.");
        }

        [Theory, ClusterRequestAutoData]
        public async Task ProcessAsync_ClusterRequest_ReturnsClusterResource(Fixture fixture, ExtensibilityOperationRequest request)
        {
            var apiResourceList = fixture
                .Customize(new SampleClusterApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(this.testOutput, httpContext =>
                httpContext.Response.WriteAsJsonAsync(apiResourceList));

            var sut = server.InjectKubeConfig(request);

            var resource = await sut.ProcessAsync(CancellationToken.None);

            resource.Namespace.Should().BeNull();
        }

        [Theory, NamespacedRequestAutoData]
        public async Task ProcessAsync_NamespacedRequest_ReturnsNamespacedResource(Fixture fixture, ExtensibilityOperationRequest request)
        {
            var apiResourceList = fixture
                .Customize(new SampleNamespacedApiResourceCustomization())
                .Create<V1APIResourceList>();

            await using var server = await MockKubernetesApiServer.StartAsync(this.testOutput, httpContext =>
                httpContext.Response.WriteAsJsonAsync(apiResourceList));

            var sut = server.InjectKubeConfig(request);

            var resource = await sut.ProcessAsync(CancellationToken.None);

            resource.Namespace.Should().NotBeNull();
        }
    }
}
