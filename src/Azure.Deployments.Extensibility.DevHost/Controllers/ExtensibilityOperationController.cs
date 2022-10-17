// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.DevHost.Swagger;
using Json.Pointer;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Net.Mime;

namespace Azure.Deployments.Extensibility.DevHost.Controllers
{
    [ApiController]
    [Tags("Resource operations")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class ExtensibilityOperationController
    {
        private readonly IExtensibilityProviderRegistry registry;

        public ExtensibilityOperationController(IExtensibilityProviderRegistry registry)
        {
            this.registry = registry;
        }

        [HttpPost("/get")]
        [SwaggerOperation("Get operation", "Gets an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        public Task<ExtensibilityOperationResponse> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestAsync(request, provider => provider.GetAsync, cancellation);

        [HttpPost("/previewSave")]
        [SwaggerOperation("PreviewSave operation", "Previews the result of saving an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(PreviewSaveResponseExampleProvider))]
        public Task<ExtensibilityOperationResponse> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestAsync(request, provider => provider.PreviewSaveAsync, cancellation);


        [HttpPost("/save")]
        [SwaggerOperation("Save operation", "Saves an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        public Task<ExtensibilityOperationResponse> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestAsync(request, provider => provider.SaveAsync, cancellation);

        [HttpPost("/delete")]
        [SwaggerOperation("Delete operation", "Deletes an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        public Task<ExtensibilityOperationResponse> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestAsync(request, provider => provider.DeleteAsync, cancellation);

        private async Task<ExtensibilityOperationResponse> HandleRequestAsync(ExtensibilityOperationRequest request, Func<IExtensibilityProvider, ExtensibilityOperation> operationSelector, CancellationToken cancellationToken)
        {
            var providerName = request.Import.Provider;
            var provider = registry.TryGetExtensibilityProvider(providerName);

            if (provider is null)
            {
                return new ExtensibilityOperationErrorResponse(
                    new ExtensibilityError(
                        "UnknownExtensibilityProvider",
                        JsonPointer.Parse($"/imports/provider"),
                        @$"Unknown extensibility provider: ""{providerName}""."));
            }

            var operation = operationSelector.Invoke(provider);
            var response = await operation.Invoke(request, cancellationToken);

            return response;
        }
    }
}
