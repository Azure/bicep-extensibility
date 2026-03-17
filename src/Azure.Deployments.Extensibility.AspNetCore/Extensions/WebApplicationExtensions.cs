// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions;

/// <summary>
/// Internal extension methods for configuring extensibility middleware and routing on <see cref="WebApplication"/>.
/// </summary>
internal static class WebApplicationExtensions
{
    /// <summary>
    /// Adds the extensibility middleware pipeline, including exception handling, request culture, and request correlation.
    /// </summary>
    public static WebApplication UseExtensionPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseWhen(
            ctx => ctx.Request.Path.Value?.Contains("/resource/", StringComparison.OrdinalIgnoreCase) == true ||
                   ctx.Request.Path.Value?.Contains("/longRunningOperation/", StringComparison.OrdinalIgnoreCase) == true,
            branch =>
            {
                branch.UseMiddleware<RequestCultureMiddleware>();
                branch.UseMiddleware<RequestCorrelationMiddleware>();
            });

        return app;
    }

    /// <summary>
    /// Maps the resource action endpoints (preview, createOrUpdate, get, delete).
    /// </summary>
    public static WebApplication MapResourceActions(this WebApplication app)
    {
        app.MapPost("/{extensionVersion}/resource/preview", HandlerDispatcher.DispatchResourcePreviewHandlerAsync);
        app.MapPost("/{extensionVersion}/resource/createOrUpdate", HandlerDispatcher.DispatchResourceCreateOrUpdateHandlerAsync);
        app.MapPost("/{extensionVersion}/resource/get", HandlerDispatcher.DispatchResourceGetHandlerAsync);
        app.MapPost("/{extensionVersion}/resource/delete", HandlerDispatcher.DispatchResourceDeleteHandlerAsync);

        return app;
    }

    /// <summary>
    /// Maps the long-running operation get endpoint.
    /// </summary>
    public static WebApplication MapLongRunningOperationActions(this WebApplication app)
    {
        app.MapPost("/{extensionVersion}/longRunningOperation/get", HandlerDispatcher.DispatchLongRunningOperationGetHandlerAsync);

        return app;
    }
}
