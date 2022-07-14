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
            var properties = JsonSerializer.SerializeToNode(resource.Properties)!.AsObject();
            var uri = GetUriFromType(resource.Type, resource.Properties);

            var response = await _graphHttpClient.GetAsync(uri, graphToken.GetString()!, cancellationToken);

            return new ExtensibilityOperationSuccessResponse(request.Resource with { Properties = JsonSerializer.Deserialize<JsonElement>(response) });
        }

        public async Task<ExtensibilityOperationResponse> ProcessSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken)
        {
            var import = ModelMapper.MapToGeneric(request.Import);
            var graphToken = import.Config.GetProperty("graphToken");
            var resource = ModelMapper.MapToGeneric(request.Resource);
            var uri = GetUriFromType(resource.Type, resource.Properties);
            var properties = JsonSerializer.SerializeToNode(resource.Properties)!.AsObject();

            // name in request body is not accepted by Graph therefore removing
            // TODO: Looks kinda ugly maybe refactor somehow
            properties.Remove("name");

            var response = await _graphHttpClient.PutAsync(uri, properties, graphToken.GetString()!, cancellationToken);

            return new ExtensibilityOperationSuccessResponse(
                request.Resource with
                {
                    Properties = JsonSerializer.Deserialize<JsonElement>(response)
                }
            );
        }
        // resourceType has format Microsoft.Graph/type1/type2@version
        public static string GetUriFromType(string resourceType, JsonElement properties)
        {
            var typesAndVersion = resourceType.Split('@');
            // TODO: Check for existence of types and version

            var types = typesAndVersion[0].Split('/');
            // TODO: Check length of types should be at least 2

            // Assume for now it has two types maximum
            var uri = $"{types[1]}(uniqueName='{properties.GetProperty("name")}')";
            if (types.Length == 3)
            {
                uri = $"{uri}/{types[2]}/$ref";
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
    }
}

