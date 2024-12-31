// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using AutoFixture;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.TestFixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Unit
{
    public class KubernetesExtensionTests
    {
        [Theory, AutoMoqData]
        internal async Task DeleteResourceAsync_ConfigIdMissing_ReturnsBadRequest(
            Fixture fixture,
            [Frozen] Mock<IModelValidator<ResourceReference>> resourceReferenceValidatorMock,
            [Frozen] Mock<IK8sClient> k8sClientMock,
            [Frozen] Mock<IK8sClientFactory> k8sClientFactoryMock,
            KubernetesExtension sut)
        {
            resourceReferenceValidatorMock.Setup(x => x.Validate(It.IsAny<ResourceReference>())).Returns((Error?)null);
            k8sClientMock.Setup(x => x.ServerHost).Returns(fixture.Create<string>());
            k8sClientFactoryMock.Setup(x => x.CreateAsync(It.IsAny<JsonObject>())).ReturnsAsync(k8sClientMock.Object);

            var resourceReference = fixture.Build<ResourceReference>()
                .With(x => x.Type, "apps/Deployment")
                .With(x => x.ApiVersion, "v1")
                .With(x => x.Identifiers, new JsonObject
                {
                    ["metadata"] = new JsonObject
                    {
                        ["name"] = fixture.Create<string>(),
                    }
                })
                .With(
                    x => x.Config, new JsonObject
                    {
                        ["kubeConfig"] = fixture.Create<string>()
                    })
                .With(x => x.ConfigId, (string?)null)
                .Create();

            var result = await sut.DeleteResourceAsync(new DefaultHttpContext(), resourceReference, default);

            var errorData = result.Should().BeOfType<BadRequest<ErrorData>>().Subject.Value!;

            errorData.Should().NotBeNull();
            errorData.Error.Code.Should().Be("InvalidConfigId");
            errorData.Error.Message.Should().Be("The resource reference is missing a config ID.");
        }

        [Theory, AutoMoqData]
        internal async Task DeleteResourceAsync_ClusterMismatch_ReturnsUnprocessableEntity(
            Fixture fixture,
            DefaultHttpContext httpContext,
            [Frozen] Mock<IModelValidator<ResourceReference>> resourceReferenceValidatorMock,
            [Frozen] Mock<IK8sClient> k8sClientMock,
            [Frozen] Mock<IK8sClientFactory> k8sClientFactoryMock,
            KubernetesExtension sut)
        {
            resourceReferenceValidatorMock.Setup(x => x.Validate(It.IsAny<ResourceReference>())).Returns((Error?)null);
            k8sClientMock.Setup(x => x.ServerHost).Returns(fixture.Create<string>());
            k8sClientFactoryMock.Setup(x => x.CreateAsync(It.IsAny<JsonObject>())).ReturnsAsync(k8sClientMock.Object);

            var resourceReference = fixture.Build<ResourceReference>()
                .With(x => x.Type, "apps/Deployment")
                .With(x => x.ApiVersion, "v1")
                .With(x => x.Identifiers, new JsonObject
                {
                    ["metadata"] = new JsonObject
                    {
                        ["name"] = fixture.Create<string>(),
                    }
                })
                .With(x => x.Config, new JsonObject
                {
                    ["kubeConfig"] = fixture.Create<string>()
                })
                .With(x => x.ConfigId, fixture.Create<string>())
                .Create();

            var result = await sut.DeleteResourceAsync(httpContext, resourceReference, default);

            var errorData = result.Should().BeOfType<UnprocessableEntity<ErrorData>>().Subject.Value!;

            errorData.Should().NotBeNull();
            errorData.Error.Code.Should().Be("ClusterMismatch");
        }
    }
}
