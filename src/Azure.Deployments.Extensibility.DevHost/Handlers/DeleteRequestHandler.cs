// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.DevHost.Swagger;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Azure.Deployments.Extensibility.DevHost.Handlers
{
    public class DeleteRequestHandler : ExtensibilityRequestHandler
    {
        public DeleteRequestHandler()
            : base("/delete", provider => provider.DeleteAsync)
        {
        }

        [SwaggerOperation("Delete operation", "Deletes an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse))]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        protected override Task<object> HandleAsync(ExtensibilityOperationRequest request, IExtensibilityProviderRegistry registry, CancellationToken cancellationToken) =>
            base.HandleAsync(request, registry, cancellationToken);
    }
}
