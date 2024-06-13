// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration.TestFixtures;
using Microsoft.AspNetCore.Http.HttpResults;
using FluentAssertions;
using Azure.Deployments.Extensibility.Core.V2.Json;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration
{
    /*
     * To run the tests locally:
     *   - Install Docker and minikube (https://minikube.sigs.k8s.io/docs/start/)
     *   - Run "minikube start"
     *
     * To clean up resources created by the tests:
     *   - Run "minikube delete"
     */
    public class KubernetesExtensionPreviewTests : KubernetesExtensionTestBase
    {
        [Theory, K8sDeploymentSpecificationAutoData]
        public async Task PreviewDeployment_UnderDefaultNamespace_PerformsServerDryRun(K8sDeploymentSpecification specification)
        {
            // Act.
            var result = await PreviewResourceCreateOrUpdateAsync(specification);

            // Assert.
            var resource = result.Should().BeOfType<Ok<Resource>>().Subject.Value!;

            resource.Should().NotBeNull();
            resource.Properties.GetPropertyValue<string>("/metadata/namespace").Should().Be("default");
            resource.Properties.TryGetPropertyNode("/metadata/uid").Should().NotBeNull();
        }

        [Theory, K8sDeploymentSpecificationAutoData(generateNamespace: true)]
        public async Task PreviewDeployment_UnderNonDefaultExistingNamespace_PerformsServerDryRun(K8sDeploymentSpecification specification)
        {
            // Arrange.
            await CreateOrUpdateNamespaceAsync(specification.NamespaceInMetadata);

            // Act.
            var result = await PreviewResourceCreateOrUpdateAsync(specification);

            // Aseert.
            var resource = result.Should().BeOfType<Ok<Resource>>().Subject.Value!;

            resource.Should().NotBeNull();
            resource.Properties.GetPropertyValue<string>("/metadata/namespace").Should().Be(specification.NamespaceInMetadata);
            resource.Properties.TryGetPropertyNode("/metadata/uid").Should().NotBeNull();
        }

        [Theory, K8sDeploymentSpecificationAutoData(generateNamespace: true)]
        public async Task PreviewDeployment_UnderNonexistentNamespace_PerformsClientDryRun(K8sDeploymentSpecification specification)
        {
            // Act.
            var result = await PreviewResourceCreateOrUpdateAsync(specification);

            // Assert.
            var resource = result.Should().BeOfType<Ok<Resource>>().Subject.Value!;

            resource.Should().NotBeNull();
            resource.Properties.GetPropertyValue<string>("/metadata/namespace").Should().Be(specification.NamespaceInMetadata);
            resource.Properties.TryGetPropertyNode("/metadata/uid").Should().BeNull();
        }
    }
}
