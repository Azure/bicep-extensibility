// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Models;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

using static Microsoft.AspNetCore.Http.TypedResults;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

internal static class BicepExtensionHandlerDispatcher
{
    public static async Task<IResult> DispatchResourcePreviewAsync(
        string extensionVersion,
        ResourcePreviewSpecification request,
        [FromServices] HandlerInvoker handlerInvoker,
        CancellationToken cancellationToken)
    {
        var response = await handlerInvoker.InvokeResourcePreviewAsync(extensionVersion, request, cancellationToken);

        return response.Match(
            resourcePreview => Ok(resourcePreview),
            ErrorResponseToHttpResult);
    }

    public static async Task<IResult> DispatchResourceCreateOrUpdateAsync(
        string extensionVersion,
        ResourceSpecification request,
        [FromServices] HandlerInvoker handlerInvoker,
        CancellationToken cancellationToken)
    {
        var response = await handlerInvoker.InvokeResourceCreateOrUpdateAsync(extensionVersion, request, cancellationToken);

        return response.Match(
            resource => Ok(resource),
            longRunningOperation => Accepted(uri: (string?)null, longRunningOperation),
            ErrorResponseToHttpResult);
    }

    public static async Task<IResult> DispatchResourceGetAsync(
        string extensionVersion,
        ResourceReference request,
        [FromServices] HandlerInvoker handlerInvoker,
        CancellationToken cancellationToken)
    {
        var response = await handlerInvoker.InvokeResourceGetAsync(extensionVersion, request, cancellationToken);

        return response.Match(
            resource => resource is null
                ? NotFound(new ErrorResponse(new Error(
                    "ResourceNotFound",
                    $"The resource of type {request.Type} with api version {request.ApiVersion} and identifiers {request.Identifiers} was not found.")))
                : Ok(resource),
            ErrorResponseToHttpResult);
    }

    public static async Task<IResult> DispatchResourceDeleteAsync(
        string extensionVersion,
        ResourceReference request,
        [FromServices] HandlerInvoker handlerInvoker,
        CancellationToken cancellationToken)
    {
        var response = await handlerInvoker.InvokeResourceDeleteAsync(extensionVersion, request, cancellationToken);

        return response.Match(
            resource => resource is null ? NoContent() : Ok(resource),
            longRunningOperation => Accepted(uri: (string?)null, longRunningOperation),
            ErrorResponseToHttpResult);
    }

    public static async Task<IResult> DispatchLongRunningOperationGetAsync(
        string extensionVersion,
        JsonObject request,
        [FromServices] HandlerInvoker handlerInvoker,
        CancellationToken cancellationToken)
    {
        var response = await handlerInvoker.InvokeLongRunningOperationGetAsync(extensionVersion, request, cancellationToken);

        return response.Match(
            longRunningOperation => Ok(longRunningOperation),
            ErrorResponseToHttpResult);
    }

    private static IResult ErrorResponseToHttpResult(ErrorResponse errorResponse) => errorResponse is HttpErrorResponse httpErrorResponse
        ? TypedResults.Json(httpErrorResponse.AsErrorResponse(), statusCode: httpErrorResponse.StatusCode)
        : BadRequest(errorResponse);
}
