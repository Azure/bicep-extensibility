// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Models;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Semver;
using System.Text.Json.Nodes;

using static Microsoft.AspNetCore.Http.TypedResults;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

internal static class HandlerDispatcher
{
    public static async Task<IResult> DispatchResourcePreviewHandlerAsync(
        string extensionVersion, ResourcePreviewSpecification specification, IEnumerable<IResourcePreviewHandler> handlers, CancellationToken cancellationToken)
    {
        var handler = ResolveHandler(extensionVersion, handlers);
        var handlerResult = await handler.HandleAsync(specification, cancellationToken);

        return handlerResult.Match(
            resourcePreview => Ok(resourcePreview),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }

    public static async Task<IResult> DispatchResourceCreateOrUpdateHandlerAsync(
        string extensionVersion, ResourceSpecification specification, IEnumerable<IResourceCreateOrUpdateHandler> handlers, CancellationToken cancellationToken)
    {
        var handler = ResolveHandler(extensionVersion, handlers);
        var handlerResult = await handler.HandleAsync(specification, cancellationToken);

        return handlerResult.Match(
            resource => Ok(resource),
            longRunningOperation => Accepted(uri: (string?)null, longRunningOperation),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }

    public static async Task<IResult> DispatchResourceGetHandlerAsync(
        string extensionVersion, ResourceReference reference, IEnumerable<IResourceGetHandler> handlers, CancellationToken cancellationToken)
    {
        var handler = ResolveHandler(extensionVersion, handlers);
        var handlerResult = await handler.HandleAsync(reference, cancellationToken);

        return handlerResult.Match(
            resource => resource is not null ? Ok(resource) : NotFound(),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }

    public static async Task<IResult> DispatchResourceDeleteHandlerAsync(
        string extensionVersion, ResourceReference reference, IEnumerable<IResourceDeleteHandler> handlers, CancellationToken cancellationToken)
    {
        var handler = ResolveHandler(extensionVersion, handlers);
        var handlerResult = await handler.HandleAsync(reference, cancellationToken);

        return handlerResult.Match(
            resource => resource is not null ? Ok(resource) : NoContent(),
            longRunningOperation => Accepted(uri: (string?)null, longRunningOperation),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }

    // An error result here indicates an issue retrieving the operation status (e.g., network errors),
    // NOT a failure of the operation itself. Operation failures are reported via the operation's status field.
    public static async Task<IResult> DispatchLongRunningOperationGetHandlerAsync(
        string extensionVersion, JsonObject operationHandle, IEnumerable<ILongRunningOperationGetHandler> handlers, CancellationToken cancellationToken)
    {
        var handler = ResolveHandler(extensionVersion, handlers);
        var handlerResult = await handler.HandleAsync(operationHandle, cancellationToken);

        return handlerResult.Match(
            longRunningOperation => Ok(longRunningOperation),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }

    private static THandler ResolveHandler<THandler>(string extensionVersion, IEnumerable<THandler> handlers)
        where THandler : IHandler
    {
        if (!SemVersion.TryParse(extensionVersion, out var targetVersion))
        {
            throw new ErrorResponseException("InvalidExtensionVersion", $"The provided extension version '{extensionVersion}' is not a valid semantic version.");
        }

        var matchingHandlers = handlers.Where(x => x.SupportedExtensionVersions.Contains(targetVersion)).ToArray();

        if (matchingHandlers.Length == 0)
        {
            throw new InvalidOperationException($"No handler found for extension version '{extensionVersion}'.");
        }

        if (matchingHandlers.Length > 1)
        {
            throw new InvalidOperationException($"Multiple handlers found for extension version '{extensionVersion}'.");
        }

        return matchingHandlers[0];
    }

    private static IResult ErrorResponseToHttpResult(ErrorResponse errorResponse) => errorResponse is HttpErrorResponse httpErrorResponse
        ? TypedResults.Json(httpErrorResponse.AsErrorResponse(), statusCode: httpErrorResponse.StatusCode)
        : BadRequest(errorResponse);
}
