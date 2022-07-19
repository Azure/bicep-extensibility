// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Azure.Deployments.Extensibility.Core.Json;
using FluentAssertions;
using Moq;
using System.Net;
using System.Text;
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
        [InlineData("graphToken", "groupName", "Microsoft.Graph/groups/members@2022-06-15-preview")]
        public async void GetAsync_Succeed(string graphToken, string name, string resourceType)
        {
            var request = ConstructRequest(graphToken, name, resourceType);
            var properties = request.Resource.Properties;
            var expectedUri = GraphProvider.GenerateGetUri(resourceType, properties);
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(expectedUri, graphToken, CancellationToken.None))
                .Returns(Task.FromResult(ConstructResponse(HttpStatusCode.Accepted, properties.ToString())));

            var provider = new GraphProvider(mockHttpClient.Object);
            var response = await provider.GetAsync(request, CancellationToken.None);

            mockHttpClient.Verify(c => c.GetAsync(expectedUri, graphToken, CancellationToken.None), Times.Once);
            response.Should().NotBeNull();
            response.Resource.Should().NotBeNull();
            Assert.Equal(properties.ToString(), response.Resource!.Properties.ToString());
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, "This is a bad request", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData(HttpStatusCode.InternalServerError, "Internal Error", "Microsoft.Graph/groups@2022-06-15-preview")]
        public async void GetAsync_ShouldThrowException(HttpStatusCode errorCode, string errorMessage, string resourceType)
        {
            var graphToken = "graphToken";
            var name = "name";
            var request = ConstructRequest(graphToken, name, resourceType);
            var properties = request.Resource.Properties;
            var expectedUri = GraphProvider.GenerateGetUri(resourceType, properties);
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(expectedUri, graphToken, CancellationToken.None))
                .Returns(Task.FromResult(ConstructResponse(errorCode, errorMessage)));

            var provider = new GraphProvider(mockHttpClient.Object);
            var testAction = async () => await provider.GetAsync(request, CancellationToken.None);
            var exception = await Assert.ThrowsAsync<ExtensibilityException>(testAction);

            exception.Errors.First().Message.Should().Be(errorMessage);
            exception.Errors.First().Code.Should().Be(((int)errorCode).ToString());
        }

        [Theory]
        [InlineData("graphToken", "userName", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData("graphToken", "groupName", "Microsoft.Graph/groups@2022-06-15-preview")]
        [InlineData("graphToken", "groupName", "Microsoft.Graph/groups/members@2022-06-15-preview")]
        public async void SaveAsync_CreateShouldSucceed(string graphToken, string name, string resourceType)
        {
            var request = ConstructRequest(graphToken, name, resourceType);
            var properties = request.Resource.Properties;
            var propertiesString = JsonSerializer.SerializeToNode(new { displayName = name })!.AsObject().ToString();
            var getUri = GraphProvider.GenerateGetUri(resourceType, properties);
            var postUri = GraphProvider.GeneratePostUri(resourceType, properties);
            var getResponseContent = ConstructResponseIdContent(getUri, "");
            var getStatusCode = getUri.StartsWith("users") ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            var mockHttpClient = Repository.Create<GraphHttpClient>(); 
            mockHttpClient
                .Setup(c => c.GetAsync(getUri, graphToken, CancellationToken.None))
                .Returns(Task.FromResult(ConstructResponse(getStatusCode, getResponseContent)));
            mockHttpClient
                .Setup(c => c.PostAsync(
                    postUri,
                    It.Is<JsonObject>(p => p.ToString() == propertiesString!),
                    graphToken,
                    CancellationToken.None)
                )
                .Returns(Task.FromResult(ConstructResponse(HttpStatusCode.Created, properties.ToString())));

            var provider = new GraphProvider(mockHttpClient.Object);
            var response = await provider.SaveAsync(request, CancellationToken.None);

            mockHttpClient.Verify(c => c.GetAsync(getUri, graphToken, CancellationToken.None), Times.Once);
            mockHttpClient.Verify(c => c.PostAsync(
                postUri,
                It.Is<JsonObject>(p => p.ToString() == propertiesString),
                graphToken,
                CancellationToken.None
                ),Times.Once
            );
            response.Should().NotBeNull();
            response.Resource.Should().NotBeNull();
            Assert.Equal(properties.ToString(), response.Resource!.Properties.ToString());
        }

        [Theory]
        [InlineData("graphToken", "userName", "userid", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData("graphToken", "groupName", "groupid", "Microsoft.Graph/groups@2022-06-15-preview")]
        [InlineData("graphToken", "groupName", "groupid", "Microsoft.Graph/groups/members@2022-06-15-preview")]
        public async void SaveAsync_UpdateShouldSucceed(string graphToken, string name, string id, string resourceType)
        {
            var request = ConstructRequest(graphToken, name, resourceType);
            var properties = request.Resource.Properties;
            var propertiesString = JsonSerializer.SerializeToNode(new { displayName = name })!.AsObject().ToString();
            var getUri = GraphProvider.GenerateGetUri(resourceType, properties);
            var patchUri = GraphProvider.GeneratePatchUri(resourceType, id);
            var getResponseContent = ConstructResponseIdContent(getUri, id);
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(getUri, graphToken, CancellationToken.None))
                .Returns(Task.FromResult(ConstructResponse(HttpStatusCode.OK, getResponseContent)));
            mockHttpClient
                .Setup(c => c.PatchAsync(
                    patchUri,
                    It.Is<JsonObject>(p => p.ToString() == propertiesString!),
                    graphToken,
                    CancellationToken.None)
                )
                .Returns(Task.FromResult(ConstructResponse(HttpStatusCode.NoContent, "{}")));

            var provider = new GraphProvider(mockHttpClient.Object);
            var response = await provider.SaveAsync(request, CancellationToken.None);

            mockHttpClient.Verify(c => c.GetAsync(getUri, graphToken, CancellationToken.None), Times.Once);
            mockHttpClient.Verify(c => c.PatchAsync(
                patchUri,
                It.Is<JsonObject>(p => p.ToString() == propertiesString),
                graphToken,
                CancellationToken.None
                ), Times.Once
            );
            response.Should().NotBeNull();
            response.Resource.Should().NotBeNull();
            Assert.Equal(properties.ToString(), response.Resource!.Properties.ToString());
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, "users/userName", "userId")]
        [InlineData(HttpStatusCode.NotFound, "users/userName", "")]
        [InlineData(HttpStatusCode.OK, "groups/groupName", "groupId")]
        [InlineData(HttpStatusCode.OK, "groups/groupName", "")]
        public void GetIdIfExists_ShouldSucceed(HttpStatusCode statusCode, string uri, string expectedId)
        {
            string content = ConstructResponseIdContent(uri, expectedId);
            var response = ConstructResponse(statusCode, content);

            var resultId = GraphProvider.GetIdIfExists(response, uri);

            resultId.Should().Be(expectedId);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, "users/userName", "Internal Server Error")]
        [InlineData(HttpStatusCode.NotFound, "groups/groupName", "Resource Not Found")]
        public void GetIdIfExists_ShouldThrowException(HttpStatusCode statusCode, string uri, string errorMessage)
        {
            var response = ConstructResponse(statusCode, errorMessage);

            void testAction() => GraphProvider.GetIdIfExists(response, uri);

            var exception = Assert.Throws<GraphHttpException>(testAction);
            exception.StatusCode.Should().Be((int)statusCode);
            exception.Message.Should().Be(errorMessage);
        }

        [Theory]
        [InlineData("userPrincipalName", "Microsoft.Graph/users@2022-06-15-preview", "users/userPrincipalName?$select=id")]
        [InlineData("groupDisplayName", "Microsoft.Graph/groups@2022-06-15-preview", "groups?$filter=displayName eq 'groupDisplayName'&$select=id")]
        [InlineData("groupDisplayName", "Microsoft.Graph/groups/members@2022-06-15-preview", "groups?$filter=displayName eq 'groupDisplayName'&$select=id")]
        public void GenerateGetUri_ShouldReturnExpectedUri(string name, string resourceType, string expectedUri)
        {
            var properties = JsonSerializer.SerializeToElement(new
            {
                name = name,
                displayName = name,
            });
            var resultUri = GraphProvider.GenerateGetUri(resourceType, properties);

            resultUri.Should().Be(expectedUri);
        }

        [Theory]
        [InlineData("userName", "Microsoft.Graph/users@2022-06-15-preview", "users")]
        [InlineData("groupName", "Microsoft.Graph/groups/members@2022-06-15-preview", "groups/groupName/members/$ref")]
        [InlineData("servicePrincipalName/appRoleName", "Microsoft.Graph/servicePrincipals/applroleAssignments@2022-06-15-preview", "servicePrincipals/servicePrincipalName/applroleAssignments")]
        public void GeneratePostUri_ShouldReturnExpectedUri(string name, string resourceType, string expectedUri)
        {
            var properties = JsonSerializer.SerializeToElement(new { name = name });
            var resultUri = GraphProvider.GeneratePostUri(resourceType, properties);

            resultUri.Should().Be(expectedUri);
        }

        [Theory]
        [InlineData("userId", "Microsoft.Graph/users@2022-06-15-preview", "users/userId")]
        [InlineData("groupId", "Microsoft.Graph/groups/members@2022-06-15-preview", "groups/groupId")]
        public void GeneratePatchUri_ShouldReturnExpectedUri(string id, string resourceType, string expectedUri)
        {
            var resultUri = GraphProvider.GeneratePatchUri(resourceType, id);

            resultUri.Should().Be(expectedUri);
        }

        [Theory]
        [InlineData("userName", "Microsoft.Graph@2022-06-15-preview")]
        [InlineData("groupName", "InvalidType")]
        public void GeneratePostUri_ShouldThrowException(string name, string resourceType)
        {
            var properties = JsonSerializer.SerializeToElement(new { name = name });
            void testAction() => GraphProvider.GeneratePostUri(resourceType, properties);

            Assert.Throws<IndexOutOfRangeException>(testAction);
        }

        [Theory]
        [InlineData("userName", "Microsoft.Graph@2022-06-15-preview")]
        [InlineData("groupName", "InvalidType")]
        public void GenerateGetUri_ShouldThrowException(string name, string resourceType)
        {
            var properties = JsonSerializer.SerializeToElement(new
            {
                name = name,
                displayName = name,
            });
            void testAction() => GraphProvider.GeneratePostUri(resourceType, properties);

            Assert.Throws<IndexOutOfRangeException>(testAction);
        }

        [Theory]
        [InlineData("userId", "Microsoft.Graph@2022-06-15-preview")]
        [InlineData("groupId", "InvalidType")]
        public void GeneratePatchUri_ShouldThrowException(string id, string resourceType)
        {
            void testAction() => GraphProvider.GeneratePatchUri(resourceType, id);

            Assert.Throws<IndexOutOfRangeException>(testAction);
        }

        [Fact(Skip ="Figure out password update")]
        /*
         * Status:
         * 1. Can create user
         */
        public async void CreateUser_ShouldSucceed()
        {
            var graphToken = "";
            var principalName = "testUser4PrincipalName@xgk22.onmicrosoft.com";
            var resourceType = "Microsoft.Graph/users@2022-06-15-preview";

            var request = ConstructRequest(graphToken, principalName, resourceType);
            var properties = request.Resource.Properties;

            var provider = new GraphProvider();
            var response = await provider.SaveAsync(request, CancellationToken.None);
        }

        [Fact(Skip = "This seems to be working")]
        /*
         * Status:
         */
        public async void CreateOrUpdateGroups_ShouldSucceed()
        {
            var graphToken = "";
            var name = "TestGroup2";
            var resourceType = "Microsoft.Graph/groups@2022-06-15-preview";

            var request = ConstructRequest(graphToken, name, resourceType);
            var properties = request.Resource.Properties;

            var provider = new GraphProvider();
            var response = await provider.SaveAsync(request, CancellationToken.None);
        }

        private ExtensibilityOperationRequest ConstructRequest(string graphToken = "", string name = "", string resourceType = "")
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
                properties.Add("displayName", name);
            }
/*            var groupPropertiesObject = new
            {
                name = name,
                displayName = name,
                mailEnabled = false,
                mailNickname = $"{name}NickName222",
                securityEnabled = true
            };
        var userPropertiesObject = new
            {
                name = name,
                accountEnabled = true,
                displayName = $"{name}DisplayNameUpdated222",
                mailNickname = "testmainnickname",
                onPremisesImmutableId = $"{name}onPremisesImmutableId",
                userPrincipalName = name,
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = false,
                    forceChangePasswordNextSignInWithMfa = false,
                    password = "userTest4Password"
                }
            };*/
            var import = new ExtensibleImport<JsonElement>("provider", "version", JsonSerializer.SerializeToElement(config));
            var resource = new ExtensibleResource<JsonElement>(resourceType, JsonSerializer.SerializeToElement(properties));
            var request = new ExtensibilityOperationRequest(import, resource);

            
            return request;
        }

        private HttpResponseMessage ConstructResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage(statusCode);
            response.Content = new StringContent(content, Encoding.UTF8, "application/json");

            return response;
        }

        private string ConstructResponseIdContent(string uri, string id)
        {
            string content;
            var idObject = new { id = id };

            if (uri.StartsWith("users"))
            {
                content = string.IsNullOrEmpty(id) ? "" : JsonSerializer.Serialize(idObject);
            }
            else
            {
                var body = string.IsNullOrEmpty(id) ?
                    new { value = new object[] { } } :
                    new { value = new object[] { idObject } };
                content = JsonSerializer.Serialize(body);
            }

            return content;
        }
    }
}
