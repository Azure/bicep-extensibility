// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.DevHost.Swagger;
using Json.Pointer;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Azure.Deployments.Extensibility.DevHost.Handlers
{
    public abstract class ExtensibilityRequestHandler
    {
        private readonly string routePattern;

        private readonly Func<IExtensibilityProvider, ExtensibilityOperation> providerOperationSelector;

        public ExtensibilityRequestHandler(string routePattern, Func<IExtensibilityProvider, ExtensibilityOperation> providerOperationSelector)
        {
            this.routePattern = routePattern;
            this.providerOperationSelector = providerOperationSelector;
        }

        public RouteHandlerBuilder RegisterRoute(IEndpointRouteBuilder builder) =>
            builder.MapPost(this.routePattern, this.HandleAsync).WithTags("Resource operations");

        protected virtual async Task<object> HandleAsync(ExtensibilityOperationRequest request, IExtensibilityProviderRegistry registry, CancellationToken cancellationToken)
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

            var operation = this.providerOperationSelector.Invoke(provider);
            var response = await operation.Invoke(request, cancellationToken);

            return response;
        }
    }
}
