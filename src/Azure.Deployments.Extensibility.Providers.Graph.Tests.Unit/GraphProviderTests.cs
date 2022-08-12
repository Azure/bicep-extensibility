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
        [InlineData("graphInternalData", "userName", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData("graphInternalData", "spName/appRoleName", "Microsoft.Graph/servicePrincipals/appRoleAssignments@2022-06-15-preview")]
        public async void PreviewSaveAsync_ShouldSucceed(string graphInternalData, string name, string resourceType)
        {
            var request = ConstructRequest(graphInternalData, name, resourceType);

            var provider = new GraphProvider(Repository.Create<GraphHttpClient>().Object);
            var response = await provider.PreviewSaveAsync(request, CancellationToken.None);
            var successResponse = response.Should().BeOfType<ExtensibilityOperationSuccessResponse>().Subject;

            successResponse.Should().NotBeNull();
            successResponse.Resource.Should().NotBeNull();
            Assert.Equal(request.Resource.Properties.ToString(), successResponse.Resource!.Properties.ToString());
        }

        [Theory]
        [InlineData("", "userName", "Microsoft.Graph/users@2022-06-15-preview", "Required properties [\"graphInternalData\"] were not present.")]
        [InlineData("graphInternalData", "", "Microsoft.Graph/users@2022-06-15-preview", "Required properties [\"name\"] were not present.")]
        [InlineData("graphInternalData", "userName", "Microsoft.Graph@2022-06-15-preview", "Value does not match the regular expression")]
        [InlineData("graphInternalData", "userName", "Microsoft.Graph/user", "Value does not match the regular expression")]
        public async void PreviewSaveAsync_ThrowsError(string graphInternalData, string name, string resourceType, string errorMessage)
        {
            var request = ConstructRequest(graphInternalData, name, resourceType);

            var provider = new GraphProvider(Repository.Create<GraphHttpClient>().Object);
            var testAction = async () => await provider.PreviewSaveAsync(request, CancellationToken.None);
            var exception = await Assert.ThrowsAsync<ExtensibilityException>(testAction);

            exception.Errors.First().Message.Should().StartWith(errorMessage);
        }

        [Theory]
        [InlineData("userName", "userId", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData("groupName", "groupId", "Microsoft.Graph/groups@2022-06-15-preview")]
        [InlineData("spName/appRoleAssignmentsName", "spId/appRoleId", "Microsoft.Graph/servicePrincipals/appRoleAssignments@2022-06-15-preview")]
        public async void GetAsync_Succeed(string name, string id, string resourceType)
        {
            var graphInternalData = "graphInternalData";
            var request = ConstructRequest(graphInternalData, name, resourceType);
            var properties = request.Resource.Properties;
            var expectedUri = GraphProvider.GenerateGetUri(resourceType, properties);
            var propertiesObject = BuildPropertiesObject(name, resourceType);
            var responseContent = ConstructResponseContent(expectedUri, id, propertiesObject);
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(expectedUri, graphInternalData, CancellationToken.None))
                .Returns(Task.FromResult(ConstructResponse(HttpStatusCode.Accepted, responseContent)));

            var provider = new GraphProvider(mockHttpClient.Object);
            var response = await provider.GetAsync(request, CancellationToken.None);
            var successResponse = response.Should().BeOfType<ExtensibilityOperationSuccessResponse>().Subject;

            mockHttpClient.Verify(c => c.GetAsync(expectedUri, graphInternalData, CancellationToken.None), Times.Once);
            successResponse.Should().NotBeNull();
            successResponse.Resource.Should().NotBeNull();
            Assert.Equal(propertiesObject.ToJsonString(), successResponse.Resource!.Properties.ToString());
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, "This is a bad request", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData(HttpStatusCode.InternalServerError, "Internal Error", "Microsoft.Graph/groups@2022-06-15-preview")]
        public async void GetAsync_ShouldThrowExceptionIfHttpError(HttpStatusCode errorCode, string errorMessage, string resourceType)
        {
            var graphInternalData = "graphInternalData";
            var request = ConstructRequest(graphInternalData, "name", resourceType);
            var properties = request.Resource.Properties;
            var expectedUri = GraphProvider.GenerateGetUri(resourceType, properties);
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(expectedUri, graphInternalData, CancellationToken.None))
                .Returns(Task.FromResult(ConstructResponse(errorCode, errorMessage)));

            var provider = new GraphProvider(mockHttpClient.Object);
            var testAction = async () => await provider.GetAsync(request, CancellationToken.None);
            var exception = await Assert.ThrowsAsync<ExtensibilityException>(testAction);

            exception.Errors.First().Message.Should().Be(errorMessage);
            exception.Errors.First().Code.Should().Be(((int)errorCode).ToString());
        }

        [Theory]
        [InlineData("groupName", "Microsoft.Graph/groups@2022-06-15-preview")]
        [InlineData("spName/appRoleAssignmentsName", "Microsoft.Graph/servicePrincipals/appRoleAssignments@2022-06-15-preview")]
        public async void GetAsync_ShouldThrowExceptionIfEmptyResource(string name, string resourceType)
        {
            var graphInternalData = "graphInternalData";
            var request = ConstructRequest(graphInternalData, name, resourceType);
            var properties = request.Resource.Properties;
            var expectedUri = GraphProvider.GenerateGetUri(resourceType, properties);
            var propertiesObject = BuildPropertiesObject(name, resourceType);
            var responseContent = ConstructResponseContent(expectedUri, "", propertiesObject);
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(expectedUri, graphInternalData, CancellationToken.None))
                .Returns(Task.FromResult(ConstructResponse(HttpStatusCode.Accepted, responseContent)));

            var provider = new GraphProvider(mockHttpClient.Object);
            var testAction = async () => await provider.GetAsync(request, CancellationToken.None);
            var exception = await Assert.ThrowsAsync<ExtensibilityException>(testAction);

            exception.Errors.First().Code.Should().Be("ResourceNotFound");
            exception.Errors.First().Message.Should().Be("Cannot find requested resource.");
        }

        [Theory]
        [InlineData("userName", "", "Microsoft.Graph/users@2022-06-15-preview", 1)]
        [InlineData("groupName", "", "Microsoft.Graph/groups@2022-06-15-preview", 1)]
        [InlineData("groupName/userid", "userid", "Microsoft.Graph/groups/members@2022-06-15-preview", 0)]
        [InlineData("groupName/userid", "", "Microsoft.Graph/groups/members@2022-06-15-preview", 1)]
        public async void SaveAsync_CreateShouldSucceed(string name, string id, string resourceType, int postTimes)
        {
            var graphInternalData = "graphInternalData";
            var request = ConstructRequest(graphInternalData, name, resourceType);
            var properties = request.Resource.Properties;
            var propertiesObject = BuildPropertiesObject(name, resourceType);
            propertiesObject.Remove("name");
            var getUri = GraphProvider.GenerateGetUri(resourceType, properties);
            var postUri = GraphProvider.GeneratePostUri(resourceType, properties);
            var getResponseContent = ConstructResponseContent(getUri, id, propertiesObject);
            var getStatusCode = getUri.StartsWith("users") ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            var postStatusCode = postUri.EndsWith("$ref") ? HttpStatusCode.NoContent : HttpStatusCode.Created;
            var postResponseContent = postStatusCode == HttpStatusCode.NoContent ? "" : properties.ToString();
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(getUri, graphInternalData, CancellationToken.None))
                .Returns(Task.FromResult(ConstructResponse(getStatusCode, getResponseContent)));
            mockHttpClient
                .Setup(c => c.PostAsync(
                    postUri,
                    It.Is<JsonObject>(p => p.ToString() == propertiesObject.ToString()),
                    graphInternalData,
                    CancellationToken.None
                ))
                .Returns(Task.FromResult(ConstructResponse(postStatusCode, postResponseContent)));

            var provider = new GraphProvider(mockHttpClient.Object);
            var response = await provider.SaveAsync(request, CancellationToken.None);
            var successResponse = response.Should().BeOfType<ExtensibilityOperationSuccessResponse>().Subject;

            mockHttpClient.Verify(c => c.GetAsync(getUri, graphInternalData, CancellationToken.None), Times.Once);
            mockHttpClient.Verify(c => c.PostAsync(
                postUri,
                It.Is<JsonObject>(p => p.ToString() == propertiesObject.ToString()),
                graphInternalData,
                CancellationToken.None
                ), Times.Exactly(postTimes)
            );
            successResponse.Should().NotBeNull();
            successResponse.Resource.Should().NotBeNull();
            Assert.Equal(properties.ToString(), successResponse.Resource!.Properties.ToString());
        }

        [Theory]
        [InlineData("userName", "userid", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData("groupName", "groupid", "Microsoft.Graph/groups@2022-06-15-preview")]
        public async void SaveAsync_UpdateShouldSucceed(string name, string id, string resourceType)
        {
            var graphInternalData = "graphInternalData";
            var request = ConstructRequest(graphInternalData, name, resourceType);
            var properties = request.Resource.Properties;
            var propertiesObject = BuildPropertiesObject(name, resourceType);
            propertiesObject.Remove("name");
            var propertiesNoIdString = propertiesObject.ToString();
            var getUri = GraphProvider.GenerateGetUri(resourceType, properties);
            var patchUri = GraphProvider.GeneratePatchUri(resourceType, properties, id);
            var getResponseContent = ConstructResponseContent(getUri, id, propertiesObject);
            var mockHttpClient = Repository.Create<GraphHttpClient>();
            mockHttpClient
                .Setup(c => c.GetAsync(getUri, graphInternalData, CancellationToken.None))
                .Returns(Task.FromResult(ConstructResponse(HttpStatusCode.OK, getResponseContent)));
            mockHttpClient
                .Setup(c => c.PatchAsync(
                    patchUri,
                    It.Is<JsonObject>(p => p.ToString() == propertiesNoIdString),
                    graphInternalData,
                    CancellationToken.None)
                )
                .Returns(Task.FromResult(ConstructResponse(HttpStatusCode.NoContent, "{}")));

            var provider = new GraphProvider(mockHttpClient.Object);
            var response = await provider.SaveAsync(request, CancellationToken.None);
            var successResponse = response.Should().BeOfType<ExtensibilityOperationSuccessResponse>().Subject;

            mockHttpClient.Verify(c => c.GetAsync(getUri, graphInternalData, CancellationToken.None), Times.Exactly(2));
            mockHttpClient.Verify(c => c.PatchAsync(
                patchUri,
                It.Is<JsonObject>(p => p.ToString() == propertiesNoIdString),
                graphInternalData,
                CancellationToken.None
                ), Times.Once
            );
            successResponse.Should().NotBeNull();
            successResponse.Resource.Should().NotBeNull();
            Assert.Equal(propertiesObject.ToJsonString(), successResponse.Resource!.Properties.ToString());
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, "users/userName", JsonValueKind.Object, "userId", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData(HttpStatusCode.NotFound, "users/userName", JsonValueKind.Undefined, "", "Microsoft.Graph/users@2022-06-15-preview")]
        [InlineData(HttpStatusCode.OK, "groups/groupName", JsonValueKind.Object, "groupId", "Microsoft.Graph/groups@2022-06-15-preview")]
        [InlineData(HttpStatusCode.OK, "groups/groupName", JsonValueKind.Undefined, "", "Microsoft.Graph/groups@2022-06-15-preview")]
        [InlineData(HttpStatusCode.NotFound, "groups/groupName/members", JsonValueKind.Undefined, "", "Microsoft.Graph/groups/members@2022-06-15-preview")]
        public void GetResourceObjectIfExists_ShouldSucceed(HttpStatusCode statusCode, string uri, JsonValueKind valueKind, string expectedId, string resourceType)
        {
            string content = ConstructResponseContent(uri, expectedId, new JsonObject());
            var response = ConstructResponse(statusCode, content);

            var resultResourceObject = GraphProvider.GetResourceObjectIfExists(response, uri, resourceType);

            resultResourceObject.Should().NotBeNull();
            resultResourceObject.ValueKind.Should().Be(valueKind);
            Assert.True(
                resultResourceObject.ValueKind == JsonValueKind.Undefined ||
                resultResourceObject.GetProperty("id").ToString() == expectedId
            );
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, "users/userName", "Microsoft.Graph/users@2022-06-15-preview", "Internal Server Error")]
        [InlineData(HttpStatusCode.NotFound, "groups/groupName", "Microsoft.Graph/groups@2022-06-15-preview", "Resource Not Found")]
        public void GetResourceObjectIfExists_ShouldThrowException(HttpStatusCode statusCode, string uri, string resourceType, string errorMessage)
        {
            var response = ConstructResponse(statusCode, errorMessage);

            void testAction() => GraphProvider.GetResourceObjectIfExists(response, uri, resourceType);

            var exception = Assert.Throws<GraphHttpException>(testAction);
            exception.StatusCode.Should().Be((int)statusCode);
            exception.Message.Should().Be(errorMessage);
        }

        [Theory]
        [InlineData("userPrincipalName", "Microsoft.Graph/users@2022-06-15-preview", "users/userPrincipalName")]
        [InlineData("groupDisplayName", "Microsoft.Graph/groups@2022-06-15-preview", "groups?$filter=displayName eq 'groupDisplayName'")]
        [InlineData("spName/appRoleName", "Microsoft.Graph/servicePrincipals/appRoleAssignments@2022-06-15-preview", "servicePrincipals/spName/appRoleAssignments?$filter=id eq 'appRoleName'")]
        public void GenerateGetUri_ShouldReturnExpectedUri(string name, string resourceType, string expectedUri)
        {
            var lastName = name.Split('/').Last();
            var properties = JsonSerializer.SerializeToElement(new
            {
                name = name,
                displayName = lastName,
                userPrincipalName = lastName,
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
        [InlineData("userName", "userId", "Microsoft.Graph/users@2022-06-15-preview", "users/userId")]
        [InlineData("spName/appRoleAssignmentsName", "appRoleId", "Microsoft.Graph/servicePrincipals/appRoleAssignments@2022-06-15-preview", "servicePrincipals/spName/appRoleAssignments/appRoleId")]
        public void GeneratePatchUri_ShouldReturnExpectedUri(string name, string id, string resourceType, string expectedUri)
        {
            var properties = JsonSerializer.SerializeToElement(new {
                name = name
            });
            var resultUri = GraphProvider.GeneratePatchUri(resourceType, properties, id);

            resultUri.Should().Be(expectedUri);
        }

        [Theory]
        [InlineData("userName", "Microsoft.Graph@2022-06-15-preview", "Resource types have invalid format.")]
        [InlineData("userName", "Microsoft.Graph/users@2022-06-15-preview", "Alternate key does not exist.")]
        [InlineData("groupName", "InvalidType", "Resource types and version have invalid format.")]
        public void GenerateGetUri_ShouldThrowException(string name, string resourceType, string errorMessage)
        {
            var properties = JsonSerializer.SerializeToElement(new
            {
                name = name,
                displayName = name,
            });
            void testAction() => GraphProvider.GenerateGetUri(resourceType, properties);

            var exception = Assert.Throws<ExtensibilityException>(testAction);
            exception.Errors.First().Message.Should().Be(errorMessage);
        }

        private ExtensibilityOperationRequest ConstructRequest(string graphInternalData, string name, string resourceType)
        {
            var config = string.IsNullOrEmpty(graphInternalData) ?
                JsonSerializer.SerializeToElement(new { randomKey = "random" }) :
                JsonSerializer.SerializeToElement(new { graphInternalData = graphInternalData });
            var properties = BuildPropertiesObject(name, resourceType);
            
            var import = new ExtensibleImport<JsonElement>("provider", "version", config);
            var resource = new ExtensibleResource<JsonElement>(resourceType, JsonSerializer.SerializeToElement(properties));
            var request = new ExtensibilityOperationRequest(import, resource);

            return request;
        }

        private JsonObject BuildPropertiesObject(string name, string resourceType)
        {
            var lastName = name.Split('/').Last();
            var properties = JsonSerializer.SerializeToNode(new
            {
                displayName = lastName,
            })!.AsObject();

            if (!string.IsNullOrEmpty(name))
            {
                properties.Add("name", name);
            }

            if (resourceType.Contains("users"))
            {
                properties.Add("userPrincipalName", lastName);
            }

            return properties;
        }

        private HttpResponseMessage ConstructResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage(statusCode);
            response.Content = new StringContent(content, Encoding.UTF8, "application/json");

            return response;
        }

        private string ConstructResponseContent(string uri, string id, JsonObject properties)
        {
            string content = "";

            if (!string.IsNullOrEmpty(id))
            {
                properties.Add("id", id);
            }

            if (uri.StartsWith("users"))
            {
                content = string.IsNullOrEmpty(id) ? "" : JsonSerializer.Serialize(properties);
            }
            else
            {
                var body = string.IsNullOrEmpty(id) ?
                    new { value = new object[] { } } :
                    new { value = new object[] { properties } };
                content = JsonSerializer.Serialize(body);
            }

            return content;
        }
    }
}
