// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Attributes;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Customizations;
using FluentAssertions;
using Xunit;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration
{
    // To run the integration tests locally, you need to have Docker and minikube installed.
    // You must start Docker before running the tests. When the tests are not executed in CI,
    // MinikubeFixture will take care of starting and deleting the minikube cluster before
    // and after running all test cases.
    public class KubernetesProviderTests
    {
        [Theory, AzureVoteBackDeploymentRequestAutoData]
        public async Task SaveAsync_AzureVoteBackDeployment_Succeeds(ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var response = await sut.SaveAsync(request, CancellationToken.None);

            response.Should().BeOfType<ExtensibilityOperationSuccessResponse>();
        }

        [Theory, AzureVoteBackServiceRequestAutoData]
        public async Task SaveAsync_AzureVoteBackService_Succeeds(ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var response = await sut.SaveAsync(request, CancellationToken.None);

            response.Should().BeOfType<ExtensibilityOperationSuccessResponse>();
        }

        [Theory, RandomNamespaceRequestAutoData]
        public async Task GetAsync_SavedNamespace_Succeeds(ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            await sut.SaveAsync(request, CancellationToken.None);

            var response = await sut.GetAsync(request, CancellationToken.None);

            var successResponse = response.Should().BeOfType<ExtensibilityOperationSuccessResponse>().Subject;

            successResponse.Resource.Properties.GetProperty("metadata").TryGetProperty("uid", out _).Should().BeTrue();
        }

        [Theory, RandomNamespaceRequestAutoData]
        public async Task PreviewSaveAsync_WithExistingNamespace_PerformsServerSideDryRun(ExtensibilityOperationRequest namespaceRequest, KubernetesProvider sut)
        {
            await sut.SaveAsync(namespaceRequest, CancellationToken.None);

            var @namespace = ModelMapper.MapToConcrete<KubernetesResourceProperties>(namespaceRequest.Resource).Properties.Metadata.Name;
            var secretRequest = new Fixture()
                .Customize(new SecretRequestCustomization(@namespace))
                .Create<ExtensibilityOperationRequest>();

            var response = await sut.PreviewSaveAsync(secretRequest, CancellationToken.None);

            var successResponse = response.Should().BeOfType<ExtensibilityOperationSuccessResponse>().Subject;
            var metadata = successResponse.Resource.Properties.GetProperty("metadata");

            metadata.TryGetProperty("uid", out _).Should().BeTrue();
            metadata.TryGetProperty("namespace", out var namespaceInResponse).Should().BeTrue();
            namespaceInResponse.GetString().Should().Be(@namespace);

            metadata.TryGetProperty("labels", out var labels).Should().BeTrue();
            labels.TryGetProperty("labelOne", out var labelOne).Should().BeTrue();
            labelOne.GetString().Should().Be("valueOne");
            labels.TryGetProperty("labelTwo", out var labelTwo).Should().BeTrue();
            labelTwo.GetString().Should().Be("valueTwo");
        }

        [Theory, AutoData]
        public async Task PreviewSaveAsync_WithoutExistingNamespace_PerformsClientSideDryRun(Fixture fixture, KubernetesProvider sut)
        {
            var nonexistentNamespace = fixture.Create<string>();
            var secretRequest = new Fixture()
                .Customize(new SecretRequestCustomization(nonexistentNamespace))
                .Create<ExtensibilityOperationRequest>();

            var response = await sut.PreviewSaveAsync(secretRequest, CancellationToken.None);

            var successResponse = response.Should().BeOfType<ExtensibilityOperationSuccessResponse>().Subject;
            var metadata = successResponse.Resource.Properties.GetProperty("metadata");

            metadata.TryGetProperty("uid", out _).Should().BeFalse();
            metadata.TryGetProperty("namespace", out var namespaceInResponse).Should().BeTrue();
            namespaceInResponse.GetString().Should().Be(nonexistentNamespace);

            metadata.TryGetProperty("labels", out var labels).Should().BeTrue();
            labels.TryGetProperty("labelOne", out var labelOne).Should().BeTrue();
            labelOne.GetString().Should().Be("valueOne");
            labels.TryGetProperty("labelTwo", out var labelTwo).Should().BeTrue();
            labelTwo.GetString().Should().Be("valueTwo");
        }

        [Theory, AzureVoteBackDeploymentRequestAutoData]
        public async Task DeleteAsync_DeletedCluster_ReturnsDeploymentTargetNotFoundError(ExtensibilityOperationRequest request, KubernetesProvider sut)
        {
            var resourceCreateResponse = await sut.SaveAsync(request, CancellationToken.None);
            var successCreateResponse = resourceCreateResponse.Should().BeOfType<ExtensibilityOperationSuccessResponse>().Subject;

            // Delete the cluster
            try
            {
                // TODO(kylealbert): Before delete, need to backup the ~/.minikube/profiles folder

                await MinikubeFixture.ExecuteMinikubeCommandAsync("delete");

                // TODO(kylealbert): After delete, need to restore the ~/.minikube/profiles folder
                // TODO(kylealbert): Perform a live test in canary once flags are flipped.

                // Try to delete the resource.
                var deleteRequest = new ExtensibilityOperationRequest(Import: request.Import, Resource: successCreateResponse.Resource);

                try
                {
                    var response = await sut.DeleteAsync(deleteRequest, CancellationToken.None);
                    Assert.Fail("An exception was expected.");
                }
#pragma warning disable CS0168 // Variable is declared but never used
                catch (Exception ex)
#pragma warning restore CS0168 // Variable is declared but never used
                {
                    // Minikube delete + restore profile so k8s config to client can be constructed:
                    // System.Net.Http.HttpRequestException: No connection could be made because the target machine actively refused it. (127.0.0.1:61377) NULL status code.
                    //  System.Net.Sockets.SocketException: No connection could be made because the target machine actively refused it.
                    //    ex.SockerErrorCode = ConnnectionRefused, ex.ErrorCode = 10061,
                    throw;
                }
            }
            finally
            {
                await MinikubeFixture.ExecuteMinikubeCommandAsync("start"); // restore
            }
        }
    }
}
