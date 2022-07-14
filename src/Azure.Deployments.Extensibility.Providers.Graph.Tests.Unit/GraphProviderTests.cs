// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Azure.Deployments.Extensibility.Core.Json;
using FluentAssertions;
using Moq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Providers.Graph.Tests.Unit
{
    public class GraphProviderTests
    {
        private static readonly MockRepository Repository = new MockRepository(MockBehavior.Strict);

        [Theory]
        [InlineData("graphToken", "userName", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData("graphToken", "groupName", "Microsoft.Graph/groups/member@2022-06-15-preview")]
        public async void GetAsync_Succeed(string graphToken, string name, string resourceType)
        {
            var request = GenerateRequest(graphToken, name, resourceType);
            var properties = request.Resource.Properties;
            var expectedUri = GraphProvider.GetUriFromType(resourceType, properties);
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(expectedUri, graphToken, CancellationToken.None))
                .Returns(Task.FromResult(properties.ToString()));           
            
            var provider = new GraphProvider(mockHttpClient.Object);
            var response = await provider.GetAsync(request, CancellationToken.None);

            mockHttpClient.Verify(c => c.GetAsync(expectedUri, graphToken, CancellationToken.None), Times.Once);
            response.Should().NotBeNull();
            response.Resource.Should().NotBeNull();
            Assert.Equal(properties.ToString(), response.Resource!.Properties.ToString());
        }

        [Fact]
        public async void GetAsync_ShouldThrowException()
        {
            var graphToken = "graphToken";
            var name = "name";
            var resourceType = "Microsoft.Graph/users@2022-06-15-preview";
            var errorMessage = "This is a bad request";
            var errorCode = 400;
            var request = GenerateRequest(graphToken, name, resourceType);
            var properties = request.Resource.Properties;
            var expectedUri = GraphProvider.GetUriFromType(resourceType, properties);
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(expectedUri, graphToken, CancellationToken.None))
                .Throws(new GraphHttpException(errorCode, errorMessage));

            var provider = new GraphProvider(mockHttpClient.Object);
            var testAction = async () => await provider.GetAsync(request, CancellationToken.None);
            var exception = await Assert.ThrowsAsync<ExtensibilityException>(testAction);
            
            exception.Errors.First().Message.Should().Be(errorMessage);
            exception.Errors.First().Code.Should().Be(errorCode.ToString());
        }

        [Theory]
        [InlineData("userName", "Microsoft.Graph/users@2022-06-15-preview", "users(uniqueName='userName')")]
        [InlineData("groupName", "Microsoft.Graph/groups/members@2022-06-15-preview", "groups(uniqueName='groupName')/members/$ref")]
        public void GetUriFromType_ShouldReturnExpectedUri(string name, string resourceType, string expectedUri)
        {
            var properties = JsonSerializer.SerializeToElement(new { name = name });
            var resultUri = GraphProvider.GetUriFromType(resourceType, properties);

            resultUri.Should().Be(expectedUri);
        }

        [Theory]
        [InlineData("userName", "Microsoft.Graph@2022-06-15-preview")]
        [InlineData("groupName", "InvalidType")]
        public void GetUriFromType_ShouldThrowException(string name, string resourceType)
        {
            var properties = JsonSerializer.SerializeToElement(new { name = name });
            Action testAction = () => GraphProvider.GetUriFromType(resourceType, properties);

            Assert.Throws<IndexOutOfRangeException>(testAction);
        }

        private ExtensibilityOperationRequest GenerateRequest(string graphToken = "", string name = "", string resourceType = "")
        {
            var config = new JsonObject();
            if (!String.IsNullOrEmpty(graphToken))
            {
                config.Add("graphToken", graphToken);
            }

            var properties = new JsonObject();
            if (!String.IsNullOrEmpty(name))
            {
                properties.Add("name", name);
            }

            var import = new ExtensibleImport<JsonElement>("provider", "version", JsonSerializer.SerializeToElement(config));
            var resource = new ExtensibleResource<JsonElement>(resourceType, JsonSerializer.SerializeToElement(properties));
            var request = new ExtensibilityOperationRequest(import, resource);

            
            return request;
        }
    }
}
