// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration.TestFixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration
{
    public class KubernetesExtensionGetTests : KubernetesExtensionTestBase
    {
        [Theory, AutoData]
        public async Task GetNamespace_Existing_Succeeds(Fixture fixture)
        {
            // Arrange.
            var namespaceName = fixture.Create<string>();
            var specification = new K8sNamespaceSpecification(namespaceName);

            await CreateOrUpdateNamespaceAsync(specification);

            var reference = new ResourceReference(
                specification.Type,
                specification.ApiVersion,
                specification.Properties.AsObject(),
                specification.Config);

            // Act.
            var result = await GetResourceAsync(reference);

            // Assert.
            var resource = result.Should().BeOfType<Ok<Resource>>().Subject.Value!;

            resource.Should().NotBeNull();
            resource.Properties.GetPropertyValue<string>("/metadata/name").Should().Be(namespaceName);
        }

        [Theory, AutoData]
        public async Task GetNamespace_Nonexistent_NotFound(Fixture fixture)
        {
            // Arrange.
            var namespaceName = fixture.Create<string>();
            var specification = new K8sNamespaceSpecification(namespaceName);
            var reference = new ResourceReference(
                specification.Type,
                specification.ApiVersion,
                specification.Properties.AsObject(),
                specification.Config);

            // Act
            var result = await GetResourceAsync(reference);

            // Assert.
            var errorData = result.Should().BeOfType<NotFound<ErrorData>>().Subject.Value!;

            errorData.Error.Code.Should().Be("ObjectNotFound");
            errorData.Error.Message.Should().Be($"The referenced Kubernetes object (GroupVersionKind=v1/Namespace, Name={namespaceName}) was not found.");
        }

        [Theory, K8sDeploymentSpecificationAutoData]
        public async Task GetDeployment_Existing_Succeeds(K8sDeploymentSpecification specification)
        {
            // Arrange.
            await CreateOrUpdateResourceAsync(specification);

            var identifiers = new JsonObject
            {
                ["metadata"] = specification.Properties["metadata"]?.DeepClone(),
            };

            var reference = new ResourceReference(
                specification.Type,
                specification.ApiVersion,
                identifiers,
                specification.Config);

            // Act.
            var result = await GetResourceAsync(reference);

            // Assert.
            var resource = result.Should().BeOfType<Ok<Resource>>().Subject.Value!;

            resource.Should().NotBeNull();
            resource.Properties.GetPropertyValue<string>("/metadata/name").Should().Be(specification.Name);
            resource.Properties.GetPropertyValue<string>("/metadata/namespace").Should().Be("default");
            resource.Properties.TryGetPropertyNode("/metadata/uid").Should().NotBeNull();
        }

        [Theory, K8sDeploymentSpecificationAutoData]
        public async Task GetDeployment_Nonexistent_NotFound(K8sDeploymentSpecification specification)
        {
            // Arrange.
            var identifiers = new JsonObject
            {
                ["metadata"] = specification.Properties["metadata"]?.DeepClone(),
            };

            var reference = new ResourceReference(
                specification.Type,
                specification.ApiVersion,
                identifiers,
                specification.Config);

            // Act.
            var result = await GetResourceAsync(reference);

            // Assert.
            var errorData = result.Should().BeOfType<NotFound<ErrorData>>().Subject.Value!;

            errorData.Error.Code.Should().Be("ObjectNotFound");
            errorData.Error.Message.Should().Be($"The referenced Kubernetes object (GroupVersionKind=apps/v1/Deployment, Name={specification.Name}, Namespace=default) was not found.");
        }
    }
}
