// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Graph;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Json;

namespace Azure.Deployments.Extensibility.Providers.Graph
{
    public class GraphProvider : IExtensibilityProvider
    {
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
            var properties = JsonSerializer.SerializeToNode(resource.Properties)!.AsObject();
            var uri = GetUriFromType(resource.Type, properties);

            var graphClient = new GraphHttpClient(graphToken.GetString()!, cancellationToken);
            var response = await graphClient.GetAsync(uri);

            return new ExtensibilityOperationSuccessResponse(request.Resource with { Properties = JsonSerializer.Deserialize<JsonElement>(response) });
        }

        public async Task<ExtensibilityOperationResponse> ProcessSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var import = ModelMapper.MapToGeneric(request.Import);
            var graphToken = import.Config.GetProperty("graphToken");
            var resource = ModelMapper.MapToGeneric(request.Resource);
            var properties = JsonSerializer.SerializeToNode(resource.Properties)!.AsObject();
            var uri = GetUriFromType(resource.Type, properties);

            // name in request body is not accepted by Graph therefore removing
            // TODO: Looks kinda ugly maybe refactor somehow
            properties.Remove("name");

            var graphClient = new GraphHttpClient(graphToken.GetString()!, cancellationToken);
            var response = await graphClient.PutAsync(uri, properties);

            return new ExtensibilityOperationSuccessResponse(
                request.Resource with
                {
                    Properties = JsonSerializer.Deserialize<JsonElement>(response)
                }
            );
        }
        // resourceType has format Microsoft.Graph/type1/type2@version
        private string GetUriFromType(string resourceType, JsonObject properties)
        {
            var typesAndVersion = resourceType.Split('@');
            // TODO: Check for existence of types and version

            var types = typesAndVersion[0].Split('/');
            // TODO: Check length of types should be at least 2

            // Assume for now it has two types maximum
            var uri = $"{types[1]}(uniqueName='{properties["name"]}')";
            if (types.Length == 3)
            {
                uri = $"uri/{types[2]}/$ref";
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
            };
        }
    }
}

