// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Json;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Azure.Deployments.Extensibility.Core.Validators;
using Json.Pointer;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Providers.Graph
{
    public class GraphProvider : IExtensibilityProvider
    {
        public static readonly string ProviderName = "Graph";

        private static readonly string InternalError = "InternalError";
        private static readonly string DisplayName = "displayName";
        private static readonly string GraphToken = "graphToken";
        private static readonly string Name = "name"; // name is required in Bicep template for Graph extensible types
        private static readonly string Id = "id";
        private static readonly string Value = "value";
        private static readonly IReadOnlyDictionary<string, string> AlternateKeyByType = new Dictionary<string, string>()
        {
            ["users"] = "userPrincipalName",
        };

        // This is a temporary solution for POC to recognize operations on entity collections
        // TODO: Find a more consistent way and type generic way to handle the cases
        //       Waiting on https://identitydivision.visualstudio.com/Engineering/_workitems/edit/1997031
        private static readonly IReadOnlySet<string> NavigationPropertyByType = new HashSet<string>()
        {
            "Microsoft.Graph/groups/members@2022-06-15-preview",
            "Microsoft.Graph/servicePrincipals/appRoleAssignments@2022-06-15-preview",
        };

        private readonly GraphHttpClient GraphHttpClient;

        public GraphProvider()
        {
            this.GraphHttpClient = new GraphHttpClient();
        }

        public GraphProvider(GraphHttpClient client)
        {
            this.GraphHttpClient = client;
        }

        public Task<ExtensibilityOperationResponse> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensibilityOperationResponse> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            return Execute(ProcessGetAsync)(request, cancellationToken);
        }

        public Task<ExtensibilityOperationResponse> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensibilityOperationResponse> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            return Execute(ProcessSaveAsync)(request, cancellationToken);
        }

        public async Task<ExtensibilityOperationResponse> ProcessGetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var (graphToken, resource) = GetTokenAndResource(request);
            var uri = GenerateGetUri(resource.Type, resource.Properties);

            var response = await GraphHttpClient.GetAsync(uri, graphToken, cancellationToken);
            var responseContent = HandleHttpResponse(response);

            return new ExtensibilityOperationSuccessResponse(request.Resource with { Properties = JsonSerializer.Deserialize<JsonElement>(responseContent) });
        }


        /// <summary>
        ///     1. Get resource
        ///     2. Try to get resource id from response (GetIdIfExists)
        ///     2.1 Create if id is null or empty
        ///         2.1.1 Get POST uri and POST to create resource
        ///     2.2 Update resource if successfully get id
        ///         2.2.1 Get PATCH uri and PATCH to update resource
        /// </summary>
        public async Task<ExtensibilityOperationResponse> ProcessSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var (graphToken, resource) = GetTokenAndResource(request);
            var properties = JsonSerializer.SerializeToNode(resource.Properties)!.AsObject();
            HttpResponseMessage response;

            // "name" is not an accepted attribute in request to Graph therefore removing
            properties.Remove(Name);

            // 1. Get resource
            var getUri = GenerateGetUri(resource.Type, resource.Properties);
            var getResourceResponse = await GraphHttpClient.GetAsync(getUri, graphToken, cancellationToken);

            // 2. Try to get resource id from response if exists
            var id = GetIdIfExists(getResourceResponse, getUri, resource.Type);

            // Entity already exists in collection so no update.
            // TODO: Need to handle more cases when necessary
            if (!string.IsNullOrEmpty(id) && NavigationPropertyByType.Contains(resource.Type))
            {
                return new ExtensibilityOperationSuccessResponse(
                    request.Resource with
                    {
                        Properties = resource.Properties
                    }
                );
            }

            var shouldPost = string.IsNullOrEmpty(id) || NavigationPropertyByType.Contains(resource.Type);
            if (shouldPost)
            {
                // 2.1 Create if id is null or empty
                //2.1.1 Get POST uri and POST to create resource
                var postUri = GeneratePostUri(resource.Type, resource.Properties);
                response = await GraphHttpClient.PostAsync(postUri, properties, graphToken, cancellationToken);
            }
            else
            {
                // 2.2 Update resource if successfully get id
                // 2.2.1 Get PATCH uri and PATCH to update resource
                var patchUri = GeneratePatchUri(resource.Type, resource.Properties, id);
                response = await GraphHttpClient.PatchAsync(patchUri, properties, graphToken, cancellationToken);
            }

            // Return properties in request if none returned in response
            var content = HandleHttpResponse(response);
            JsonElement resultProperties;

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                if (shouldPost)
                {
                    // POST returns 204 probably means that a relation was just created
                    resultProperties = resource.Properties;
                }
                else
                {
                    // PATCH doesn't return a body. Send another request to GET the full body of the resource
                    var resourseResponse = await GraphHttpClient.GetAsync(getUri, graphToken, cancellationToken);
                    var resourceContent = HandleHttpResponse(resourseResponse);
                    resultProperties = JsonSerializer.Deserialize<JsonElement>(resourceContent);
                }
            }
            else
            {
                resultProperties = JsonSerializer.Deserialize<JsonElement>(content);
            }

            return new ExtensibilityOperationSuccessResponse(
                request.Resource with
                {
                    Properties = resultProperties
                }
            );
        }

        private (string, ExtensibleResource<JsonElement>) GetTokenAndResource(ExtensibilityOperationRequest request)
        {
            var resource = ModelMapper.MapToGeneric(request.Resource);
            var import = ModelMapper.MapToGeneric(request.Import);

            if (!import.Config.TryGetProperty(GraphToken, out var graphToken))
            {
                throw new ExtensibilityException(
                        InternalError,
                        JsonPointer.Empty,
                        "graphToken is required in config."
                    );
            }           

            return (graphToken.ToString(), resource);
        }

        /// <summary>
        ///     Returns actual id from response when
        ///     Response returns successfully
        ///     AND
        ///         there is an attribute "id" in response (for users)
        ///         OR "value" in response is not empty (for others)
        /// </summary>
        /// <exception cref="GraphHttpException">
        ///     Throws exception if not success or 404 for resources other than users
        /// </exception>
        public static string GetIdIfExists(HttpResponseMessage response, string uri, string resourceType)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            var id = "";

            if (response.IsSuccessStatusCode)
            {
                var contentElement = JsonSerializer.Deserialize<JsonElement>(content);

                if (contentElement.TryGetProperty(Id, out JsonElement idElement))
                {
                    id = idElement.ToString();
                }
                else
                {
                    var value = contentElement.GetProperty(Value);

                    if (value.GetArrayLength() > 0)
                    {
                        var firstValue = value.EnumerateArray().First();
                        id = firstValue.GetProperty(Id).ToString();
                    }
                }

            }
            else if (response.StatusCode != HttpStatusCode.NotFound || 
                !(uri.StartsWith("users") || NavigationPropertyByType.Contains(resourceType)))
            {
                throw new GraphHttpException((int)response.StatusCode, content);
            }

            return id;
        }

        /// <summary>
        ///     Generate uri to send GET request to
        ///     
        ///     TODO: Update logic when we know what format the name has
        ///             https://identitydivision.visualstudio.com/Engineering/_workitems/edit/1998150
        /// </summary>
        /// <returns> 
        ///     if type2 has alternate key: "type1/name1/type2/type2AlternateKey"
        ///     if type2 doesn't have alternate key: "type1/name1/type2?$filter=displayName eq 'type2DisplayName'"
        ///         where 'type2AlternateKey' and 'type2DisplayName' are fetched from properties
        /// </returns>
        public static string GenerateGetUri(string resourceType, JsonElement properties)
        {
            var types = GetTypes(resourceType);
            var uri = BuildUri(types, properties);
            var lastType = types.Last();

            // Special case for entity collection
            if (NavigationPropertyByType.Contains(resourceType))
            {
                var lastName = properties.GetProperty(Name).ToString().Split('/').Last();
                uri = $"{uri}?$filter={Id} eq '{lastName}'";
            }
            else
            {
                var alternateKey = AlternateKeyByType.ContainsKey(lastType) ? AlternateKeyByType[lastType] : DisplayName;
                // filter by id for groups/members
                if (properties.TryGetProperty(alternateKey, out var alternateValue))
                {
                    if (AlternateKeyByType.ContainsKey(lastType))
                    {
                        uri = $"{uri}/{alternateValue}";
                    }
                    else
                    {
                        uri = $"{uri}?$filter={DisplayName} eq '{alternateValue}'";
                    }
                }
                else
                {
                    throw new ExtensibilityException(InternalError, JsonPointer.Empty, "Alternate key does not exist.");
                }
            }
                     

            return uri;
        }

        /// <summary>
        ///     Generate uri to send PATCH request to
        ///     
        ///     TODO: Update logic when we know what format the name has
        ///             https://identitydivision.visualstudio.com/Engineering/_workitems/edit/1998150
        /// </summary>
        /// <returns> "type1/name1/type2/id" </returns>
        public static string GeneratePatchUri(string resourceType, JsonElement properties, string id)
        {
            var types = GetTypes(resourceType);
            var uri = BuildUri(types, properties);

            return $"{uri}/{id}";
        }

        /// <summary>
        ///     Generate uri to send POST request to
        ///     
        ///     TODO: Update logic when we know what format the name has
        ///             https://identitydivision.visualstudio.com/Engineering/_workitems/edit/1998150
        /// </summary>
        /// <returns> 
        ///     "type1/name1/members/$ref" for the last type is "members"
        ///     "type1/name1/type2" for other types
        /// </returns>
        public static string GeneratePostUri(string resourceType, JsonElement properties)
        {
            var types = GetTypes(resourceType);
            var uri = BuildUri(types, properties);

            // Special case for "groups/members" for now, not all types end with "members" need $ref
            // Need to find a pattern for update entity collections
            if (uri.EndsWith("members"))
            {
                uri = $"{uri}/$ref";
            }

            return uri;
        }
    

        /// <summary>
        ///     Get an array of types from resourceType
        /// </summary>
        /// <param name="resourceType"> resourceType has format Microsoft.Graph/type1/../typen@version</param>
        /// <returns> string array of [type1, type2, ..., typen] </returns>
        private static string[] GetTypes(string resourceType)
        {
            var errorMessage = "";
            var typesAndVersion = resourceType.Split('@');
            if (typesAndVersion.Length != 2)
            {
                errorMessage = "Resource types and version have invalid format.";
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                var types = typesAndVersion[0].Split('/');
                if (types.Length <= 1)
                {
                    errorMessage = "Resource types have invalid format.";
                }
                else
                {
                    return types.Skip(1).ToArray();
                }
            }

            throw new ExtensibilityException(InternalError, JsonPointer.Empty, errorMessage);
        }

        /// <summary>
        ///     Concatenate types with their names, except for the last type
        /// </summary>
        /// <param name="types"> An array of type strings</param>
        /// <param name="properties"> JsonElement of the resource </param>
        /// <returns> "type1/name1/type2/name2/type3" </returns>
        public static string BuildUri(string[] types, JsonElement properties)
        {
            var names = properties.GetProperty(Name).ToString().Split('/');
            var uri = $"{types[0]}";

            for (int i = 1; i < types.Length; ++i)
            {
                uri = $"{uri}/{names[i - 1]}/{types[i]}";
            }

            return uri;
        }

        private static ExtensibilityOperation Execute(ExtensibilityOperation operation)
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
                catch (ExtensibilityException e)
                {
                    throw e;
                }
                catch (Exception exception)
                {
                    throw new ExtensibilityException(InternalError, JsonPointer.Empty, exception.Message);
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

