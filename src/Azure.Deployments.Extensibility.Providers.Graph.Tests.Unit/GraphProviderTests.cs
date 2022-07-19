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

        /*        [Theory]
                [InlineData("graphToken", "userName", "Microsoft.Graph/users@2022-06-15-preview")]
                [InlineData("graphToken", "groupName", "Microsoft.Graph/groups/member@2022-06-15-preview")]
                public async void GetAsync_Succeed(string graphToken, string name, string resourceType)
                {
                    var request = GenerateRequest(graphToken, name, resourceType);
                    var properties = request.Resource.Properties;
                    var expectedUri = GraphProvider.GeneratePatchUri(resourceType, properties);
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
                    var expectedUri = GraphProvider.GeneratePatchUri(resourceType, properties);
                    var mockHttpClient = Repository.Create<GraphHttpClient>();
                    mockHttpClient
                        .Setup(c => c.GetAsync(expectedUri, graphToken, CancellationToken.None))
                        .Throws(new GraphHttpException(errorCode, errorMessage));

                    var provider = new GraphProvider(mockHttpClient.Object);
                    var testAction = async () => await provider.GetAsync(request, CancellationToken.None);
                    var exception = await Assert.ThrowsAsync<ExtensibilityException>(testAction);

                    exception.Errors.First().Message.Should().Be(errorMessage);
                    exception.Errors.First().Code.Should().Be(errorCode.ToString());
                }*/

        [Theory]
        [InlineData("userPrincipalName", "Microsoft.Graph/users@2022-06-15-preview", "users/userPrincipalName?$select=id")]
        [InlineData("groupDisplayName", "Microsoft.Graph/groups@2022-06-15-preview", "groups?$filter=displayName eq 'groupDisplayName'&$select=id")]
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
            var graphToken = "eyJ0eXAiOiJKV1QiLCJub25jZSI6InhCcTdXZkpsc3JPM3hTMFZ1T01Ya3FCTGNxdXNwYlhHNzdpR1QyN3UwVEUiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9jYzUxODE1Yy0yM2MyLTQ5MDYtYmIzYi0wMTU0OGU0YmUzMDMvIiwiaWF0IjoxNjU4MTg2ODIxLCJuYmYiOjE2NTgxODY4MjEsImV4cCI6MTY1ODE5MTM3NSwiYWNjdCI6MCwiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQVRCTFdNTm9GRiswUGR3MTAwbnlDYmRMMmtjYnJJY2hVakxMTEdXbkQ1bTZlV3JQMXgwTGJFaHNCME5sMjJVckNMOHB4M09TQ3N4aEVPYkVxVzRMMmF4YlFZM3U4Q1lMa2tCRk1OaGlQNS93PSIsImFtciI6WyJwd2QiLCJtZmEiXSwiYXBwX2Rpc3BsYXluYW1lIjoiR3JhcGggRXhwbG9yZXIiLCJhcHBpZCI6ImRlOGJjOGI1LWQ5ZjktNDhiMS1hOGFkLWI3NDhkYTcyNTA2NCIsImFwcGlkYWNyIjoiMCIsImZhbWlseV9uYW1lIjoiRG91IiwiZ2l2ZW5fbmFtZSI6Ikphc29uIiwiaWR0eXAiOiJ1c2VyIiwiaXBhZGRyIjoiMTMxLjEwNy4xLjE3OCIsIm5hbWUiOiJKYXNvbiBEb3UiLCJvaWQiOiJiOGU2MjM1Zi05N2U3LTQwYzktYTA2My0yNzgzOTUxNzBiZGMiLCJwbGF0ZiI6IjMiLCJwdWlkIjoiMTAwMzIwMDIwMEFEMDhFMyIsInJoIjoiMC5BWDBBWElGUnpNSWpCa203T3dGVWprdmpBd01BQUFBQUFBQUF3QUFBQUFBQUFBQ2FBQmcuIiwic2NwIjoiQWNjZXNzUmV2aWV3LlJlYWQuQWxsIEFjY2Vzc1Jldmlldy5SZWFkV3JpdGUuQWxsIEFjY2Vzc1Jldmlldy5SZWFkV3JpdGUuTWVtYmVyc2hpcCBBZG1pbmlzdHJhdGl2ZVVuaXQuUmVhZC5BbGwgQWRtaW5pc3RyYXRpdmVVbml0LlJlYWRXcml0ZS5BbGwgQWdyZWVtZW50LlJlYWQuQWxsIEFncmVlbWVudC5SZWFkV3JpdGUuQWxsIEFncmVlbWVudEFjY2VwdGFuY2UuUmVhZCBBZ3JlZW1lbnRBY2NlcHRhbmNlLlJlYWQuQWxsIEFuYWx5dGljcy5SZWFkIEFQSUNvbm5lY3RvcnMuUmVhZC5BbGwgQVBJQ29ubmVjdG9ycy5SZWFkV3JpdGUuQWxsIEFwcGxpY2F0aW9uLlJlYWQuQWxsIERldmljZU1hbmFnZW1lbnRBcHBzLlJlYWRXcml0ZS5BbGwgRGV2aWNlTWFuYWdlbWVudE1hbmFnZWREZXZpY2VzLlJlYWRXcml0ZS5BbGwgRGV2aWNlTWFuYWdlbWVudFNlcnZpY2VDb25maWcuUmVhZFdyaXRlLkFsbCBEaXJlY3RvcnkuUmVhZC5BbGwgRGlyZWN0b3J5LlJlYWRXcml0ZS5BbGwgZURpc2NvdmVyeS5SZWFkLkFsbCBlRGlzY292ZXJ5LlJlYWRXcml0ZS5BbGwgZW1haWwgR3JvdXAuUmVhZC5BbGwgR3JvdXAuUmVhZFdyaXRlLkFsbCBHcm91cE1lbWJlci5SZWFkLkFsbCBHcm91cE1lbWJlci5SZWFkV3JpdGUuQWxsIG9wZW5pZCBQcml2aWxlZ2VkQWNjZXNzLlJlYWQuQXp1cmVBREdyb3VwIFByaXZpbGVnZWRBY2Nlc3MuUmVhZFdyaXRlLkF6dXJlQURHcm91cCBwcm9maWxlIFRhc2tzLlJlYWQgVGFza3MuUmVhZC5TaGFyZWQgVGFza3MuUmVhZFdyaXRlIFRhc2tzLlJlYWRXcml0ZS5TaGFyZWQgVGVhbS5DcmVhdGUgVGVhbS5SZWFkQmFzaWMuQWxsIFRlYW1NZW1iZXIuUmVhZC5BbGwgVGVhbU1lbWJlci5SZWFkV3JpdGUuQWxsIFRlYW1NZW1iZXIuUmVhZFdyaXRlTm9uT3duZXJSb2xlLkFsbCBUZWFtc0FjdGl2aXR5LlJlYWQgVGVhbXNBY3Rpdml0eS5TZW5kIFRlYW1zQXBwSW5zdGFsbGF0aW9uLlJlYWRGb3JDaGF0IFRlYW1zQXBwSW5zdGFsbGF0aW9uLlJlYWRGb3JUZWFtIFRlYW1zQXBwSW5zdGFsbGF0aW9uLlJlYWRGb3JVc2VyIFRlYW1zQXBwSW5zdGFsbGF0aW9uLlJlYWRXcml0ZUZvckNoYXQgVGVhbXNBcHBJbnN0YWxsYXRpb24uUmVhZFdyaXRlRm9yVGVhbSBUZWFtc0FwcEluc3RhbGxhdGlvbi5SZWFkV3JpdGVGb3JVc2VyIFRlYW1zQXBwSW5zdGFsbGF0aW9uLlJlYWRXcml0ZVNlbGZGb3JDaGF0IFRlYW1zQXBwSW5zdGFsbGF0aW9uLlJlYWRXcml0ZVNlbGZGb3JUZWFtIFRlYW1zQXBwSW5zdGFsbGF0aW9uLlJlYWRXcml0ZVNlbGZGb3JVc2VyIFRlYW1TZXR0aW5ncy5SZWFkLkFsbCBUZWFtU2V0dGluZ3MuUmVhZFdyaXRlLkFsbCBUZWFtc1RhYi5DcmVhdGUgVGVhbXNUYWIuUmVhZC5BbGwgVGVhbXNUYWIuUmVhZFdyaXRlLkFsbCBUZWFtc1RhYi5SZWFkV3JpdGVGb3JDaGF0IFRlYW1zVGFiLlJlYWRXcml0ZUZvclRlYW0gVGVhbXNUYWIuUmVhZFdyaXRlRm9yVXNlciBUZWFtc1RhYi5SZWFkV3JpdGVTZWxmRm9yQ2hhdCBUZWFtc1RhYi5SZWFkV3JpdGVTZWxmRm9yVGVhbSBUZWFtc1RhYi5SZWFkV3JpdGVTZWxmRm9yVXNlciBUZWFtd29ya0RldmljZS5SZWFkLkFsbCBUZWFtd29ya0RldmljZS5SZWFkV3JpdGUuQWxsIFRlYW13b3JrVGFnLlJlYWQgVGVhbXdvcmtUYWcuUmVhZFdyaXRlIFRlcm1TdG9yZS5SZWFkLkFsbCBUZXJtU3RvcmUuUmVhZFdyaXRlLkFsbCBUaHJlYXRBc3Nlc3NtZW50LlJlYWRXcml0ZS5BbGwgVGhyZWF0SHVudGluZy5SZWFkLkFsbCBUaHJlYXRJbmRpY2F0b3JzLlJlYWQuQWxsIFRocmVhdEluZGljYXRvcnMuUmVhZFdyaXRlLk93bmVkQnkgVGhyZWF0U3VibWlzc2lvbi5SZWFkIFRocmVhdFN1Ym1pc3Npb24uUmVhZC5BbGwgVGhyZWF0U3VibWlzc2lvbi5SZWFkV3JpdGUgVGhyZWF0U3VibWlzc2lvbi5SZWFkV3JpdGUuQWxsIFRocmVhdFN1Ym1pc3Npb25Qb2xpY3kuUmVhZFdyaXRlLkFsbCBUcnVzdEZyYW1ld29ya0tleVNldC5SZWFkLkFsbCBUcnVzdEZyYW1ld29ya0tleVNldC5SZWFkV3JpdGUuQWxsIFVuaWZpZWRHcm91cE1lbWJlci5SZWFkLkFzR3Vlc3QgVXNlci5FeHBvcnQuQWxsIFVzZXIuSW52aXRlLkFsbCBVc2VyLk1hbmFnZUlkZW50aXRpZXMuQWxsIFVzZXIuUmVhZCBVc2VyLlJlYWQuQWxsIFVzZXIuUmVhZEJhc2ljLkFsbCBVc2VyLlJlYWRXcml0ZSBVc2VyLlJlYWRXcml0ZS5BbGwgVXNlckFjdGl2aXR5LlJlYWRXcml0ZS5DcmVhdGVkQnlBcHAgVXNlckF1dGhlbnRpY2F0aW9uTWV0aG9kLlJlYWQgVXNlckF1dGhlbnRpY2F0aW9uTWV0aG9kLlJlYWQuQWxsIFVzZXJBdXRoZW50aWNhdGlvbk1ldGhvZC5SZWFkV3JpdGUgVXNlckF1dGhlbnRpY2F0aW9uTWV0aG9kLlJlYWRXcml0ZS5BbGwgVXNlck5vdGlmaWNhdGlvbi5SZWFkV3JpdGUuQ3JlYXRlZEJ5QXBwIFVzZXJUaW1lbGluZUFjdGl2aXR5LldyaXRlLkNyZWF0ZWRCeUFwcCBXaW5kb3dzVXBkYXRlcy5SZWFkV3JpdGUuQWxsIFdvcmtmb3JjZUludGVncmF0aW9uLlJlYWQuQWxsIFdvcmtmb3JjZUludGVncmF0aW9uLlJlYWRXcml0ZS5BbGwgQXBwQ2F0YWxvZy5SZWFkLkFsbCBBcHBDYXRhbG9nLlJlYWRXcml0ZS5BbGwgQXBwQ2F0YWxvZy5TdWJtaXQgQXBwUm9sZUFzc2lnbm1lbnQuUmVhZFdyaXRlLkFsbCBBcHBsaWNhdGlvbi5SZWFkV3JpdGUuQWxsIEFwcHJvdmFsLlJlYWQuQWxsIEFwcHJvdmFsLlJlYWRXcml0ZS5BbGwgQXR0YWNrU2ltdWxhdGlvbi5SZWFkLkFsbCBBdWRpdExvZy5SZWFkLkFsbCBBdXRoZW50aWNhdGlvbkNvbnRleHQuUmVhZC5BbGwgQXV0aGVudGljYXRpb25Db250ZXh0LlJlYWRXcml0ZS5BbGwiLCJzdWIiOiJ3dk1HMTJGVktya3dER05YSUxuQ2FUTnBNQ0Y0c09heFVIMmZPRzF3Zm9RIiwidGVuYW50X3JlZ2lvbl9zY29wZSI6Ik5BIiwidGlkIjoiY2M1MTgxNWMtMjNjMi00OTA2LWJiM2ItMDE1NDhlNGJlMzAzIiwidW5pcXVlX25hbWUiOiJqYXNvbmRvdUB4Z2syMi5vbm1pY3Jvc29mdC5jb20iLCJ1cG4iOiJqYXNvbmRvdUB4Z2syMi5vbm1pY3Jvc29mdC5jb20iLCJ1dGkiOiItZ296V0wyRHMwU1BVQmJCRG42QUFBIiwidmVyIjoiMS4wIiwid2lkcyI6WyI3NDk1ZmRjNC0zNGM0LTRkMTUtYTI4OS05ODc4OGNlMzk5ZmQiLCI5MzYwZmViNS1mNDE4LTRiYWEtODE3NS1lMmEwMGJhYzQzMDEiLCI4MzI5MTUzYi0zMWQwLTQ3MjctYjk0NS03NDVlYjNiYzVmMzEiLCJlOGNlZjZmMS1lNGJkLTRlYTgtYmMwNy00YjhkOTUwZjQ0NzciLCI1ZDZiNmJiNy1kZTcxLTQ2MjMtYjRhZi05NjM4MGEzNTI1MDkiLCJmZmQ1MmZhNS05OGRjLTQ2NWMtOTkxZC1mYzA3M2ViNTlmOGYiLCI1ZjIyMjJiMS01N2MzLTQ4YmEtOGFkNS1kNDc1OWYxZmRlNmYiLCJhYWY0MzIzNi0wYzBkLTRkNWYtODgzYS02OTU1MzgyYWMwODEiLCIwZjk3MWVlYS00MWViLTQ1NjktYTcxZS01N2JiOGEzZWZmMWUiLCJiZTJmNDVhMS00NTdkLTQyYWYtYTA2Ny02ZWMxZmE2M2JjNDUiLCIwOTY0YmI1ZS05YmRiLTRkN2ItYWMyOS01OGU3OTQ4NjJhNDAiLCJmZGQ3YTc1MS1iNjBiLTQ0NGEtOTg0Yy0wMjY1MmZlOGZhMWMiLCJjNGUzOWJkOS0xMTAwLTQ2ZDMtOGM2NS1mYjE2MGRhMDA3MWYiLCI5ZjA2MjA0ZC03M2MxLTRkNGMtODgwYS02ZWRiOTA2MDZmZDgiLCJiMGY1NDY2MS0yZDc0LTRjNTAtYWZhMy0xZWM4MDNmMTJlZmUiLCJhZjc4ZGMzMi1jZjRkLTQ2ZjktYmE0ZS00NDI4NTI2MzQ2YjUiLCJjZjFjMzhlNS0zNjIxLTQwMDQtYTdjYi04Nzk2MjRkY2VkN2MiLCI5Yjg5NWQ5Mi0yY2QzLTQ0YzctOWQwMi1hNmFjMmQ1ZWE1YzMiLCI3NDRlYzQ2MC0zOTdlLTQyYWQtYTQ2Mi04YjNmOTc0N2EwMmMiLCIwNTI2NzE2Yi0xMTNkLTRjMTUtYjJjOC02OGUzYzIyYjlmODAiLCIzMWU5MzlhZC05NjcyLTQ3OTYtOWMyZS04NzMxODEzNDJkMmQiLCJkMzdjOGJlZC0wNzExLTQ0MTctYmEzOC1iNGFiZTY2Y2U0YzIiLCIzOGE5NjQzMS0yYmRmLTRiNGMtOGI2ZS01ZDNkOGFiYWMxYTQiLCI3NGVmOTc1Yi02NjA1LTQwYWYtYTVkMi1iOTUzOWQ4MzYzNTMiLCIzZWRhZjY2My0zNDFlLTQ0NzUtOWY5NC01YzM5OGVmNmMwNzAiLCJlODYxMWFiOC1jMTg5LTQ2ZTgtOTRlMS02MDIxM2FiMWY4MTQiLCJmMDIzZmQ4MS1hNjM3LTRiNTYtOTVmZC03OTFhYzAyMjYwMzMiLCIyNWRmMzM1Zi04NmViLTQxMTktYjcxNy0wZmYwMmRlMjA3ZTkiLCIxNzMxNTc5Ny0xMDJkLTQwYjQtOTNlMC00MzIwNjJjYWNhMTgiLCIzYTJjNjJkYi01MzE4LTQyMGQtOGQ3NC0yM2FmZmVlNWQ5ZDUiLCI1OWQ0NmY4OC02NjJiLTQ1N2ItYmNlYi01YzM4MDllNTkwOGYiLCIzZjFhY2FkZS0xZTA0LTRmYmMtOWI2OS1mMDMwMmNkODRhZWYiLCI4OGQ4ZTNlMy04ZjU1LTRhMWUtOTUzYS05Yjk4OThiODg3NmIiLCIyOTIzMmNkZi05MzIzLTQyZmQtYWRlMi0xZDA5N2FmM2U0ZGUiLCI0NWQ4ZDNjNS1jODAyLTQ1YzYtYjMyYS0xZDcwYjVlMWU4NmUiLCJiMWJlMWMzZS1iNjVkLTRmMTktODQyNy1mNmZhMGQ5N2ZlYjkiLCI3OTBjMWZiOS03ZjdkLTRmODgtODZhMS1lZjFmOTVjMDVjMWIiLCIyYjc0NWJkZi0wODAzLTRkODAtYWE2NS04MjJjNDQ5M2RhYWMiLCI3Njk4YTc3Mi03ODdiLTRhYzgtOTAxZi02MGQ2YjA4YWZmZDIiLCI2NDRlZjQ3OC1lMjhmLTRlMjgtYjlkYy0zZmRkZTlhYTBiMWYiLCJhOWVhODk5Ni0xMjJmLTRjNzQtOTUyMC04ZWRjZDE5MjgyNmMiLCIzMTM5MmZmYi01ODZjLTQyZDEtOTM0Ni1lNTk0MTVhMmNjNGUiLCI0NDM2NzE2My1lYmExLTQ0YzMtOThhZi1mNTc4Nzg3OWY5NmEiLCJjNDMwYjM5Ni1lNjkzLTQ2Y2MtOTZmMy1kYjAxYmY4YmI2MmEiLCIxZDMzNmQyYy00YWU4LTQyZWYtOTcxMS1iMzYwNGNlM2ZjMmMiLCJlMzk3M2JkZi00OTg3LTQ5YWUtODM3YS1iYThlMjMxYzcyODYiLCI1YzRmOWRjZC00N2RjLTRjZjctOGM5YS05ZTQyMDdjYmZjOTEiLCI5YzZkZjBmMi0xZTdjLTRkYzMtYjE5NS02NmRmYmQyNGFhOGYiLCJlYjFmNGE4ZC0yNDNhLTQxZjAtOWZiZC1jN2NkZjZjNWVmN2MiLCJlNmQxYTIzYS1kYTExLTRiZTQtOTU3MC1iZWZjODZkMDY3YTciLCIxMTY0ODU5Ny05MjZjLTRjZjMtOWMzNi1iY2ViYjBiYThkY2MiLCJiNWE4ZGNmMy0wOWQ1LTQzYTktYTYzOS04ZTI5ZWYyOTE0NzAiLCI4YWMzZmM2NC02ZWNhLTQyZWEtOWU2OS01OWY0YzdiNjBlYjIiLCI4ODM1MjkxYS05MThjLTRmZDctYTljZS1mYWE0OWYwY2Y3ZDkiLCJmMmVmOTkyYy0zYWZiLTQ2YjktYjdjZi1hMTI2ZWU3NGM0NTEiLCJhYzE2ZTQzZC03YjJkLTQwZTAtYWMwNS0yNDNmZjM1NmFiNWIiLCI3Mjk4MjdlMy05YzE0LTQ5ZjctYmIxYi05NjA4ZjE1NmJiYjgiLCI4NDI0YzZmMC1hMTg5LTQ5OWUtYmJkMC0yNmMxNzUzYzk2ZDQiLCI0YTVkOGY2NS00MWRhLTRkZTQtODk2OC1lMDM1YjY1MzM5Y2YiLCI3YmU0NGM4YS1hZGFmLTRlMmEtODRkNi1hYjI2NDllMDhhMTMiLCI5NjY3MDdkMC0zMjY5LTQ3MjctOWJlMi04YzNhMTBmMTliOWQiLCI4OTJjNTg0Mi1hOWE2LTQ2M2EtODA0MS03MmFhMDhjYTNjZjYiLCI1OGExM2VhMy1jNjMyLTQ2YWUtOWVlMC05YzBkNDNjZDdmM2QiLCI2ZTU5MTA2NS05YmFkLTQzZWQtOTBmMy1lOTQyNDM2NmQyZjAiLCI5NWU3OTEwOS05NWMwLTRkOGUtYWVlMy1kMDFhY2NmMmQ0N2IiLCIxNThjMDQ3YS1jOTA3LTQ1NTYtYjdlZi00NDY1NTFhNmI1ZjciLCI0ZDZhYzE0Zi0zNDUzLTQxZDAtYmVmOS1hM2UwYzU2OTc3M2EiLCJmZTkzMGJlNy01ZTYyLTQ3ZGItOTFhZi05OGMzYTQ5YTM4YjEiLCI2MmU5MDM5NC02OWY1LTQyMzctOTE5MC0wMTIxNzcxNDVlMTAiLCJmMjhhMWY1MC1mNmU3LTQ1NzEtODE4Yi02YTEyZjJhZjZiNmMiLCIxOTRhZTRjYi1iMTI2LTQwYjItYmQ1Yi02MDkxYjM4MDk3N2QiLCJiNzlmYmY0ZC0zZWY5LTQ2ODktODE0My03NmIxOTRlODU1MDkiXSwieG1zX3N0Ijp7InN1YiI6IkM5THFRY1dUSmR6eU5uSnpZZnJLVXlkYWNtbmFGaGNJcUI4N1pXM2V3NlUifSwieG1zX3RjZHQiOjE2NTQwNjg3Mzh9.Hz9Pqp8ZOoVhNeAYh9KnIdFUJKgM4VHt9enRU_pSNEGK1m6oIkBWiNDgegdzvnsfJaX8cFmye2DchiMXbsnhmqucWCS1qnW08zNkd1nt3YP5SzKkjtuHfVYa09GPsmRfoEKBwPoBlJ0GEBNCpitHmRNrzkmXnhNh0OP2wzkQSfeBGOKZoe5UBVezU05wxEPP_dco1qk2KOc4zAPaS1eyk593ncgsEFl4k7sN7fOaX3XKRweWr1vuNyS91KnazBNiRvcAWXtvGkZMPLlH_M8dFnqjo6Hb1mLBm9PBit1_Qb2663mmbmNO69fsd_VHXVlkmsEw-VFAmSr-Pzjqlv1x0w";
            var principalName = "testUser4PrincipalName@xgk22.onmicrosoft.com";
            var resourceType = "Microsoft.Graph/users@2022-06-15-preview";

            var request = GenerateRequest(graphToken, principalName, resourceType);
            var properties = request.Resource.Properties;

            var provider = new GraphProvider();
            var response = await provider.SaveAsync(request, CancellationToken.None);
        }

        [Fact]
        /*
         * Status:
         */
        public async void CreateOrUpdateGroups_ShouldSucceed()
        {
            var graphToken = "eyJ0eXAiOiJKV1QiLCJub25jZSI6IllYcDRFUy1rSkJfV01yc0NSdnlpNk5KX3RnOEt3UElrNG1RWHllT3BSMXMiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9jYzUxODE1Yy0yM2MyLTQ5MDYtYmIzYi0wMTU0OGU0YmUzMDMvIiwiaWF0IjoxNjU4MTk0NzUwLCJuYmYiOjE2NTgxOTQ3NTAsImV4cCI6MTY1ODIwMDI5MCwiYWNjdCI6MCwiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQUZGOXpWMmt5YmNWa3pDeXdtZXRnODdRRFloMExydm1FblNYY1hwTXBGUWZzR2c0K2dlS25IYkdYV2JCSVhiOCt3ME9LbEVQSVV4QWVSZmpKWHRBb1FGeURUVVR0cDhSVld5dHB1Nnd6ajJrPSIsImFtciI6WyJwd2QiLCJtZmEiXSwiYXBwX2Rpc3BsYXluYW1lIjoiR3JhcGggRXhwbG9yZXIiLCJhcHBpZCI6ImRlOGJjOGI1LWQ5ZjktNDhiMS1hOGFkLWI3NDhkYTcyNTA2NCIsImFwcGlkYWNyIjoiMCIsImZhbWlseV9uYW1lIjoiRG91IiwiZ2l2ZW5fbmFtZSI6Ikphc29uIiwiaWR0eXAiOiJ1c2VyIiwiaXBhZGRyIjoiNC4yLjEzLjY3IiwibmFtZSI6Ikphc29uIERvdSIsIm9pZCI6ImI4ZTYyMzVmLTk3ZTctNDBjOS1hMDYzLTI3ODM5NTE3MGJkYyIsInBsYXRmIjoiMyIsInB1aWQiOiIxMDAzMjAwMjAwQUQwOEUzIiwicmgiOiIwLkFYMEFYSUZSek1JakJrbTdPd0ZVamt2akF3TUFBQUFBQUFBQXdBQUFBQUFBQUFDYUFCZy4iLCJzY3AiOiJDYWxlbmRhcnMuUmVhZFdyaXRlIENoYXQuUmVhZCBDaGF0LlJlYWRCYXNpYyBDb250YWN0cy5SZWFkV3JpdGUgRGV2aWNlTWFuYWdlbWVudFJCQUMuUmVhZC5BbGwgRGV2aWNlTWFuYWdlbWVudFNlcnZpY2VDb25maWcuUmVhZC5BbGwgRmlsZXMuUmVhZFdyaXRlLkFsbCBHcm91cC5SZWFkV3JpdGUuQWxsIElkZW50aXR5Umlza0V2ZW50LlJlYWQuQWxsIE1haWwuUmVhZCBNYWlsLlJlYWRXcml0ZSBNYWlsYm94U2V0dGluZ3MuUmVhZFdyaXRlIE5vdGVzLlJlYWRXcml0ZS5BbGwgb3BlbmlkIFBlb3BsZS5SZWFkIFBsYWNlLlJlYWQgUHJlc2VuY2UuUmVhZCBQcmVzZW5jZS5SZWFkLkFsbCBQcmludGVyU2hhcmUuUmVhZEJhc2ljLkFsbCBQcmludEpvYi5DcmVhdGUgUHJpbnRKb2IuUmVhZEJhc2ljIHByb2ZpbGUgUmVwb3J0cy5SZWFkLkFsbCBTaXRlcy5SZWFkV3JpdGUuQWxsIFRhc2tzLlJlYWRXcml0ZSBVc2VyLlJlYWQgVXNlci5SZWFkQmFzaWMuQWxsIFVzZXIuUmVhZFdyaXRlIFVzZXIuUmVhZFdyaXRlLkFsbCBlbWFpbCIsInN1YiI6Ind2TUcxMkZWS3Jrd0RHTlhJTG5DYVROcE1DRjRzT2F4VUgyZk9HMXdmb1EiLCJ0ZW5hbnRfcmVnaW9uX3Njb3BlIjoiTkEiLCJ0aWQiOiJjYzUxODE1Yy0yM2MyLTQ5MDYtYmIzYi0wMTU0OGU0YmUzMDMiLCJ1bmlxdWVfbmFtZSI6Imphc29uZG91QHhnazIyLm9ubWljcm9zb2Z0LmNvbSIsInVwbiI6Imphc29uZG91QHhnazIyLm9ubWljcm9zb2Z0LmNvbSIsInV0aSI6ImZXZjM1V21MSGtHcjNnRnljVl9rQUEiLCJ2ZXIiOiIxLjAiLCJ3aWRzIjpbIjc0OTVmZGM0LTM0YzQtNGQxNS1hMjg5LTk4Nzg4Y2UzOTlmZCIsIjkzNjBmZWI1LWY0MTgtNGJhYS04MTc1LWUyYTAwYmFjNDMwMSIsIjgzMjkxNTNiLTMxZDAtNDcyNy1iOTQ1LTc0NWViM2JjNWYzMSIsImU4Y2VmNmYxLWU0YmQtNGVhOC1iYzA3LTRiOGQ5NTBmNDQ3NyIsIjVkNmI2YmI3LWRlNzEtNDYyMy1iNGFmLTk2MzgwYTM1MjUwOSIsImZmZDUyZmE1LTk4ZGMtNDY1Yy05OTFkLWZjMDczZWI1OWY4ZiIsIjVmMjIyMmIxLTU3YzMtNDhiYS04YWQ1LWQ0NzU5ZjFmZGU2ZiIsImFhZjQzMjM2LTBjMGQtNGQ1Zi04ODNhLTY5NTUzODJhYzA4MSIsIjBmOTcxZWVhLTQxZWItNDU2OS1hNzFlLTU3YmI4YTNlZmYxZSIsImJlMmY0NWExLTQ1N2QtNDJhZi1hMDY3LTZlYzFmYTYzYmM0NSIsIjA5NjRiYjVlLTliZGItNGQ3Yi1hYzI5LTU4ZTc5NDg2MmE0MCIsImZkZDdhNzUxLWI2MGItNDQ0YS05ODRjLTAyNjUyZmU4ZmExYyIsImM0ZTM5YmQ5LTExMDAtNDZkMy04YzY1LWZiMTYwZGEwMDcxZiIsIjlmMDYyMDRkLTczYzEtNGQ0Yy04ODBhLTZlZGI5MDYwNmZkOCIsImIwZjU0NjYxLTJkNzQtNGM1MC1hZmEzLTFlYzgwM2YxMmVmZSIsImFmNzhkYzMyLWNmNGQtNDZmOS1iYTRlLTQ0Mjg1MjYzNDZiNSIsImNmMWMzOGU1LTM2MjEtNDAwNC1hN2NiLTg3OTYyNGRjZWQ3YyIsIjliODk1ZDkyLTJjZDMtNDRjNy05ZDAyLWE2YWMyZDVlYTVjMyIsIjc0NGVjNDYwLTM5N2UtNDJhZC1hNDYyLThiM2Y5NzQ3YTAyYyIsIjA1MjY3MTZiLTExM2QtNGMxNS1iMmM4LTY4ZTNjMjJiOWY4MCIsIjMxZTkzOWFkLTk2NzItNDc5Ni05YzJlLTg3MzE4MTM0MmQyZCIsImQzN2M4YmVkLTA3MTEtNDQxNy1iYTM4LWI0YWJlNjZjZTRjMiIsIjM4YTk2NDMxLTJiZGYtNGI0Yy04YjZlLTVkM2Q4YWJhYzFhNCIsIjc0ZWY5NzViLTY2MDUtNDBhZi1hNWQyLWI5NTM5ZDgzNjM1MyIsIjNlZGFmNjYzLTM0MWUtNDQ3NS05Zjk0LTVjMzk4ZWY2YzA3MCIsImU4NjExYWI4LWMxODktNDZlOC05NGUxLTYwMjEzYWIxZjgxNCIsImYwMjNmZDgxLWE2MzctNGI1Ni05NWZkLTc5MWFjMDIyNjAzMyIsIjI1ZGYzMzVmLTg2ZWItNDExOS1iNzE3LTBmZjAyZGUyMDdlOSIsIjE3MzE1Nzk3LTEwMmQtNDBiNC05M2UwLTQzMjA2MmNhY2ExOCIsIjNhMmM2MmRiLTUzMTgtNDIwZC04ZDc0LTIzYWZmZWU1ZDlkNSIsIjU5ZDQ2Zjg4LTY2MmItNDU3Yi1iY2ViLTVjMzgwOWU1OTA4ZiIsIjNmMWFjYWRlLTFlMDQtNGZiYy05YjY5LWYwMzAyY2Q4NGFlZiIsIjg4ZDhlM2UzLThmNTUtNGExZS05NTNhLTliOTg5OGI4ODc2YiIsIjI5MjMyY2RmLTkzMjMtNDJmZC1hZGUyLTFkMDk3YWYzZTRkZSIsIjQ1ZDhkM2M1LWM4MDItNDVjNi1iMzJhLTFkNzBiNWUxZTg2ZSIsImIxYmUxYzNlLWI2NWQtNGYxOS04NDI3LWY2ZmEwZDk3ZmViOSIsIjc5MGMxZmI5LTdmN2QtNGY4OC04NmExLWVmMWY5NWMwNWMxYiIsIjJiNzQ1YmRmLTA4MDMtNGQ4MC1hYTY1LTgyMmM0NDkzZGFhYyIsIjc2OThhNzcyLTc4N2ItNGFjOC05MDFmLTYwZDZiMDhhZmZkMiIsIjY0NGVmNDc4LWUyOGYtNGUyOC1iOWRjLTNmZGRlOWFhMGIxZiIsImE5ZWE4OTk2LTEyMmYtNGM3NC05NTIwLThlZGNkMTkyODI2YyIsIjMxMzkyZmZiLTU4NmMtNDJkMS05MzQ2LWU1OTQxNWEyY2M0ZSIsIjQ0MzY3MTYzLWViYTEtNDRjMy05OGFmLWY1Nzg3ODc5Zjk2YSIsImM0MzBiMzk2LWU2OTMtNDZjYy05NmYzLWRiMDFiZjhiYjYyYSIsIjFkMzM2ZDJjLTRhZTgtNDJlZi05NzExLWIzNjA0Y2UzZmMyYyIsImUzOTczYmRmLTQ5ODctNDlhZS04MzdhLWJhOGUyMzFjNzI4NiIsIjVjNGY5ZGNkLTQ3ZGMtNGNmNy04YzlhLTllNDIwN2NiZmM5MSIsIjljNmRmMGYyLTFlN2MtNGRjMy1iMTk1LTY2ZGZiZDI0YWE4ZiIsImViMWY0YThkLTI0M2EtNDFmMC05ZmJkLWM3Y2RmNmM1ZWY3YyIsImU2ZDFhMjNhLWRhMTEtNGJlNC05NTcwLWJlZmM4NmQwNjdhNyIsIjExNjQ4NTk3LTkyNmMtNGNmMy05YzM2LWJjZWJiMGJhOGRjYyIsImI1YThkY2YzLTA5ZDUtNDNhOS1hNjM5LThlMjllZjI5MTQ3MCIsIjhhYzNmYzY0LTZlY2EtNDJlYS05ZTY5LTU5ZjRjN2I2MGViMiIsIjg4MzUyOTFhLTkxOGMtNGZkNy1hOWNlLWZhYTQ5ZjBjZjdkOSIsImYyZWY5OTJjLTNhZmItNDZiOS1iN2NmLWExMjZlZTc0YzQ1MSIsImFjMTZlNDNkLTdiMmQtNDBlMC1hYzA1LTI0M2ZmMzU2YWI1YiIsIjcyOTgyN2UzLTljMTQtNDlmNy1iYjFiLTk2MDhmMTU2YmJiOCIsIjg0MjRjNmYwLWExODktNDk5ZS1iYmQwLTI2YzE3NTNjOTZkNCIsIjRhNWQ4ZjY1LTQxZGEtNGRlNC04OTY4LWUwMzViNjUzMzljZiIsIjdiZTQ0YzhhLWFkYWYtNGUyYS04NGQ2LWFiMjY0OWUwOGExMyIsIjk2NjcwN2QwLTMyNjktNDcyNy05YmUyLThjM2ExMGYxOWI5ZCIsIjg5MmM1ODQyLWE5YTYtNDYzYS04MDQxLTcyYWEwOGNhM2NmNiIsIjU4YTEzZWEzLWM2MzItNDZhZS05ZWUwLTljMGQ0M2NkN2YzZCIsIjZlNTkxMDY1LTliYWQtNDNlZC05MGYzLWU5NDI0MzY2ZDJmMCIsIjk1ZTc5MTA5LTk1YzAtNGQ4ZS1hZWUzLWQwMWFjY2YyZDQ3YiIsIjE1OGMwNDdhLWM5MDctNDU1Ni1iN2VmLTQ0NjU1MWE2YjVmNyIsIjRkNmFjMTRmLTM0NTMtNDFkMC1iZWY5LWEzZTBjNTY5NzczYSIsImZlOTMwYmU3LTVlNjItNDdkYi05MWFmLTk4YzNhNDlhMzhiMSIsIjYyZTkwMzk0LTY5ZjUtNDIzNy05MTkwLTAxMjE3NzE0NWUxMCIsImYyOGExZjUwLWY2ZTctNDU3MS04MThiLTZhMTJmMmFmNmI2YyIsIjE5NGFlNGNiLWIxMjYtNDBiMi1iZDViLTYwOTFiMzgwOTc3ZCIsImI3OWZiZjRkLTNlZjktNDY4OS04MTQzLTc2YjE5NGU4NTUwOSJdLCJ4bXNfc3QiOnsic3ViIjoiQzlMcVFjV1RKZHp5Tm5KellmcktVeWRhY21uYUZoY0lxQjg3WlczZXc2VSJ9LCJ4bXNfdGNkdCI6MTY1NDA2ODczOH0.EAETHCk6ab-TqRZTETEWOg5V1A4GTC4KJZxQXDeo3Y2yceVRdGRT8pVgvhqV5IRKsDjegdIvtxc--orT-k35e6CK4dAPaTg_y3-o40Q96_lRQ0pJegXTgZGwpYT-0W-qXwYqY-5fh9MpDFsBiXkGfpVzTgUYhYuHWa0kRB4bFU1I8NQwdYNtOg_RG0Ju5bfnB-WJhtyT1fc-Rs47bDwc7QxNAOdPed7AGreP1GcORP7rV-DwHG5pKXc8sjCBN43EWJ0XN950B8ErDRGLj6ROnFLMsyyFp_aMf2t8IMzy5NJv1ZtKLyw6PjFZb68Yb47HDmFCKU3xaJUw4dSQeiqlOw";
            var name = "TestGroup2";
            var resourceType = "Microsoft.Graph/groups@2022-06-15-preview";

            var request = GenerateRequest(graphToken, name, resourceType);
            var properties = request.Resource.Properties;

            var provider = new GraphProvider();
            var response = await provider.SaveAsync(request, CancellationToken.None);
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
            var groupPropertiesObject = new
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
            };
            var import = new ExtensibleImport<JsonElement>("provider", "version", JsonSerializer.SerializeToElement(config));
            var resource = new ExtensibleResource<JsonElement>(resourceType, JsonSerializer.SerializeToElement(groupPropertiesObject));
            var request = new ExtensibilityOperationRequest(import, resource);

            
            return request;
        }
    }
}
