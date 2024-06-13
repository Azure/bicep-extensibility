// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration.TestFixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration
{
    public class KubernetesExtensionCreateOrUpdateTests : KubernetesExtensionTestBase
    {
        [Theory, AutoData]
        public async Task CreateNamespace_ByDefault_Succeeds(Fixture fixture)
        {
            // Arrange.
            var namespaceName = fixture.Create<string>();

            // Act.
            var result = await CreateOrUpdateNamespaceAsync(namespaceName);

            // Assert.
            var resource = result.Should().BeOfType<Ok<Resource>>().Subject.Value!;

            resource.Should().NotBeNull();
            resource.Properties.GetPropertyValue<string>("/metadata/name").Should().Be(namespaceName);
        }

        [Theory, K8sDeploymentSpecificationAutoData]
        public async Task CreateDeployment_UnderDefaultNamespace_Succeeds(K8sDeploymentSpecification specification)
        {
            // Act.
            var result = await CreateOrUpdateResourceAsync(specification);

            // Assert.
            var resource = result.Should().BeOfType<Ok<Resource>>().Subject.Value!;

            resource.Should().NotBeNull();
            resource.Properties.GetPropertyValue<string>("/metadata/namespace").Should().Be("default");
            resource.Properties.TryGetPropertyNode("/metadata/uid").Should().NotBeNull();
        }

        [Theory, K8sDeploymentSpecificationAutoData(generateNamespace: true)]
        public async Task CreateDeployment_UnderNonDefaultExistingNamespace_Succeeds(K8sDeploymentSpecification specification)
        {
            // Arrange.
            await CreateOrUpdateNamespaceAsync(specification.NamespaceInMetadata);

            // Act.
            var result = await CreateOrUpdateResourceAsync(specification);

            // Assert.
            var resource = result.Should().BeOfType<Ok<Resource>>().Subject.Value!;

            resource.Should().NotBeNull();
            resource.Properties.GetPropertyValue<string>("/metadata/namespace").Should().Be(specification.NamespaceInMetadata);
            resource.Properties.TryGetPropertyNode("/metadata/uid").Should().NotBeNull();
        }

        [Theory, K8sDeploymentSpecificationAutoData(generateNamespace: true)]
        public async Task CreateDeployment_UnderNonexistentNamespace_Throws(K8sDeploymentSpecification specification)
        {
            var exception = (await Invoking(() => CreateOrUpdateResourceAsync(specification))
                .Should()
                .ThrowAsync<ErrorResponseException>()).Which;

            exception.Error.Code.Should().Be("KubernetesOperationFailure");
            exception.Error.Message.Should().Contain("NotFound");
        }

        [Theory, AutoData]
        public async Task UpdateNamespace_ByDefault_Succeeds(Fixture fixture)
        {
            // Arrange.
            var namespaceName = fixture.Create<string>();
            await CreateOrUpdateNamespaceAsync(namespaceName);

            // Act.
            var result = await CreateOrUpdateNamespaceAsync(namespaceName);

            // Assert.
            result.Should().BeOfType<Ok<Resource>>();
        }

        [Theory, K8sDeploymentSpecificationAutoData(generateNamespace: true)]
        public async Task UpdateDeployment_AddLables_Succeeds(string labelOne, string labelTwo, K8sDeploymentSpecification specification)
        {
            // Arrange.
            await CreateOrUpdateNamespaceAsync(specification.NamespaceInMetadata);
            await CreateOrUpdateResourceAsync(specification);

            specification.Properties.SetPropertyValue("/metadata/labels/labelOne", labelOne);
            specification.Properties.SetPropertyValue("/metadata/labels/labelTwo", labelTwo);

            // Act.
            var result = await CreateOrUpdateResourceAsync(specification);

            // Assert.
            var resource = result.Should().BeOfType<Ok<Resource>>().Subject.Value!;

            resource.Should().NotBeNull();
            resource.Properties.GetPropertyValue<string>("/metadata/namespace").Should().Be(specification.NamespaceInMetadata);
            resource.Properties.TryGetPropertyNode("/metadata/uid").Should().NotBeNull();
            resource.Properties.TryGetPropertyValue<string>("/metadata/labels/labelOne").Should().Be(labelOne);
            resource.Properties.TryGetPropertyValue<string>("/metadata/labels/labelTwo").Should().Be(labelTwo);
        }
    }
}
