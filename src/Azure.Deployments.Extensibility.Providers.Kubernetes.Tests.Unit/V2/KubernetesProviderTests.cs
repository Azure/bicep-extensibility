// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.V2.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Repositories;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services;
using FluentAssertions;
using Json.Pointer;
using Microsoft.AspNetCore.Http;
using Xunit;
using V2KubernetesProvider = Azure.Deployments.Extensibility.Providers.Kubernetes.V2.KubernetesProvider;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.V2
{
    public class KubernetesProviderTests
    {
        private static readonly V2KubernetesProvider Sut = new(
            new V1APIResourceCatalogServiceFactory(),
            new K8sResourceRepositoryFactory());

        [Theory, AutoData]
        public async Task CreateResourceReferenceAsync_InvalidRequestBody_Throws(string providerVersion, string resourceType)
        {
            var requestBody = new ResourceRequestBody(resourceType, [], []);

            var act = () => Sut.CreateResourceReferenceAsync(new DefaultHttpContext(), providerVersion, requestBody, CancellationToken.None);

            var exception = (await act.Should().ThrowAsync<ErrorResponseException>()).Which;
            AssertMultipleErrorsOccurred(exception.Error);
        }

        [Theory, AutoData]
        public async Task PreviewResourceCreateOrUpdateAsync_InvalidRequestBody_ReturnsBadRequest(string providerVersion, string resourceType)
        {
            var requestBody = new ResourceRequestBody(resourceType, [], []);

            var act = () => Sut.PreviewResourceCreateOrUpdateAsync(new DefaultHttpContext(), providerVersion, requestBody, CancellationToken.None);

            var exception = (await act.Should().ThrowAsync<ErrorResponseException>()).Which;
            AssertMultipleErrorsOccurred(exception.Error);
        }

        [Theory, AutoData]
        public async Task CreateOrUpdateResourceAsync_InvalidRequestBody_ReturnsBadRequest(string providerVersion, string referenceId, string resourceType)
        {
            var requestBody = new ResourceRequestBody(resourceType, [], []);

            var act = () => Sut.CreateOrUpdateResourceAsync(new DefaultHttpContext(), providerVersion, referenceId, requestBody, CancellationToken.None);

            var exception = (await act.Should().ThrowAsync<ErrorResponseException>()).Which;
            AssertMultipleErrorsOccurred(exception.Error);
        }

        private static void AssertMultipleErrorsOccurred(Error error)
        {
            error.Code.Should().Be("MultipleErrorsOccurred");
            error.Message.Should().Be("Multiple errors occurred. Please see details for more information.");
            error.Details.Should().NotBeNull();
            error.Details.Should().HaveCount(3);

            var errorDetails = error.Details!;

            errorDetails[0].Code.Should().Be("InvalidType");
            errorDetails[0].Message.Should().Be(@"Expected type to match the regular expression ^((?<group>[a-zA-Z0-9.]+)\/)?(?<kind>[a-zA-Z]+)@(?<version>[a-zA-Z0-9]+)$.");
            errorDetails[0].Target.Should().Be(JsonPointer.Create("type"));

            errorDetails[1].Code.Should().Be("InvalidProperty");
            errorDetails[1].Message.Should().Be(@"Required properties [""metadata""] are not present.");
            errorDetails[1].Target.Should().Be(JsonPointer.Create("properties"));

            errorDetails[2].Code.Should().Be("InvalidConfig");
            errorDetails[2].Message.Should().Be(@"Required properties [""kubeConfig""] are not present.");
            errorDetails[2].Target.Should().Be(JsonPointer.Create("config"));
        }
    }
}
