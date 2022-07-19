// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Json;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Azure.Deployments.Extensibility.Core.Validators;
using Json.Pointer;
using Microsoft.Graph;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Providers.Graph
{
    public class GraphProvider : IExtensibilityProvider
    {
        private GraphHttpClient _graphHttpClient;

        public GraphProvider()
        {
            this._graphHttpClient = new GraphHttpClient();
        }

        public GraphProvider(GraphHttpClient client)
        {
            this._graphHttpClient = client;
        }
        public Task<ExtensibilityOperationResponse> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensibilityOperationResponse> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            return HandleException(ProcessGetAsync)(request, cancellationToken);
        }

        public Task<ExtensibilityOperationResponse> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensibilityOperationResponse> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            return HandleException(ProcessSaveAsync)(request, cancellationToken);
        }

        public async Task<ExtensibilityOperationResponse> ProcessGetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var import = ModelMapper.MapToGeneric(request.Import);
            var graphToken = import.Config.GetProperty("graphToken");
            var resource = ModelMapper.MapToGeneric(request.Resource);
            var uri = GenerateGetUri(resource.Type, resource.Properties);

            var response = await _graphHttpClient.GetAsync(uri, graphToken.GetString()!, cancellationToken);
            var responseContent = HandleHttpResponse(response);

            return new ExtensibilityOperationSuccessResponse(request.Resource with { Properties = JsonSerializer.Deserialize<JsonElement>(responseContent) });
        }

        /*
         * 1. Get resource
         * 2. Try to get resource id from response (GetIdIfExists)
         * 2.1 Create if id is null or empty
         *  2.1.1 Get POST uri and POST to create resource
         * 2.2 Update resource if successfully get id
         *  2.2.1 Get PATCH uri and PATCH to update resource
         */
        public async Task<ExtensibilityOperationResponse> ProcessSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var import = ModelMapper.MapToGeneric(request.Import);
            var graphToken = import.Config.GetProperty("graphToken");
            var resource = ModelMapper.MapToGeneric(request.Resource);
            var properties = JsonSerializer.SerializeToNode(resource.Properties)!.AsObject();
            HttpResponseMessage response;

            // "name" is not an accepted attribute in request to Graph therefore removing
            properties.Remove("name");

            // 1. Get resource
            var getUri = GenerateGetUri(resource.Type, resource.Properties);
            var getResourceResponse = await _graphHttpClient.GetAsync(getUri, graphToken.GetString()!, cancellationToken);

            // 2. Try to get resource id from response if exists
            var id = GetIdIfExists(getResourceResponse, getUri);

            if (string.IsNullOrEmpty(id))
            {
                // 2.1 Create if id is null or empty
                //2.1.1 Get POST uri and POST to create resource
                var postUri = GeneratePostUri(resource.Type, resource.Properties);
                response = await _graphHttpClient.PostAsync(postUri, properties, graphToken.GetString()!, cancellationToken);
            }
            else
            {
                // 2.2 Update resource if successfully get id
                // 2.2.1 Get PATCH uri and PATCH to update resource
                var patchUri = GeneratePatchUri(resource.Type, id);
                response = await _graphHttpClient.PatchAsync(patchUri, properties, graphToken.GetString()!, cancellationToken);
            }

            // Return properties in request if none returned in response
            var content = HandleHttpResponse(response);
            var resultProperties = response.StatusCode == HttpStatusCode.NoContent ? resource.Properties : JsonSerializer.Deserialize<JsonElement>(content);

            return new ExtensibilityOperationSuccessResponse(
                request.Resource with
                {
                    Properties = resultProperties
                }
            );
        }

        /*
         * Returns actual id from response when
         * Response returns successfully
         * AND 
         *  there is an attribute "id" in response (for users)
         *  OR "value" in response is not empty (for others)
         */
        public static string GetIdIfExists(HttpResponseMessage response, string uri)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            var id = "";

            if (response.IsSuccessStatusCode)
            {
                var contentElement = JsonSerializer.Deserialize<JsonElement>(content);

                if (contentElement.TryGetProperty("id", out JsonElement idElement))
                {
                    id = idElement.ToString();
                }
                else
                {
                    var value = contentElement.GetProperty("value");

                    if (value.GetArrayLength() > 0)
                    {
                        var firstValue = value.EnumerateArray().First();
                        id = firstValue.GetProperty("id").ToString();
                    }
                }

            }
            else if (response.StatusCode != HttpStatusCode.NotFound || !uri.StartsWith("users"))
            {
                throw new GraphHttpException((int)response.StatusCode, content);
            }

            return id;
        }

        public static string GenerateGetUri(string resourceType, JsonElement properties)
        {
            var typesAndVersion = resourceType.Split('@');
            var types = typesAndVersion[0].Split('/');
            var uri = types[1];

            // TODO: Use `uniqueName` when ready
            if (types.Length == 2 && uri == "users")
            {
                // Get user by name
                uri = $"{uri}/{properties.GetProperty("name")}?";
            }
            else
            {
                // Get other entities by filtering displayName
                uri = $"{uri}?$filter=displayName eq '{properties.GetProperty("displayName")}'&";
            }

            // Select id only in response
            uri = $"{uri}$select=id";

            return uri;
        }

        // resourceType has format Microsoft.Graph/type1/type2@version
        public static string GeneratePatchUri(string resourceType, string id)
        {
            var typesAndVersion = resourceType.Split('@');
            var types = typesAndVersion[0].Split('/');

            // Assume for now patch only applies to one level entities. No nested resources
            // users, groups, applications, and servicePrincipals
            // TODO: Use `uniqueName` when ready
            return $"{types[1]}/{id}";
        }

        public static string GeneratePostUri(string resourceType, JsonElement properties)
        {
            var typesAndVersion = resourceType.Split('@');
            var types = typesAndVersion[0].Split('/');
            var name = properties.GetProperty("name").ToString();
            var names = name.Split('/');
            var uri = types[1];

            // types: Microsoft.Graph, groups, members
            // names: groupid
            // TODO: Use `uniqueName` when ready
            for (int i = 2; i < types.Length; i++)
            {
                uri = $"{uri}/{names[i - 2]}/{types[i]}";
            }

            if (uri.EndsWith("members"))
            {
                uri = $"{uri}/$ref";
            }

            return uri;
        }

        private static ExtensibilityOperation HandleException(ExtensibilityOperation operation)
        {
            return async (ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await operation.Invoke(request, cancellationToken);
                }
                catch (GraphHttpException exception)
                {
                    throw exception.ToExtensibilityException();
                }
                catch (Exception exception)
                {
                    throw new ExtensibilityException(
                        "InternalError",
                        JsonPointer.Empty,
                        exception.Message
                    );
                }
            };
        }

        private string HandleHttpResponse(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                return content;
            }
            else
            {
                throw new GraphHttpException((int)response.StatusCode, content);
            }
        }
    }
}

