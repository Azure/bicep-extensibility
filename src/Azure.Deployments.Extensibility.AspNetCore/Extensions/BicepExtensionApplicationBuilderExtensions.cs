// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions;

/// <summary>
/// Extension methods for configuring Bicep extension middleware.
/// </summary>
public static class BicepExtensionApplicationBuilderExtensions
{
    /// <summary>
    /// Adds request culture handling to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseBicepExtensionRequestCulture(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RequestCultureMiddleware>();
    }

    /// <summary>
    /// Adds request correlation handling to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseBicepExtensionRequestCorrelation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RequestCorrelationMiddleware>();
    }

    /// <summary>
    /// Adds the shared exception, request culture, and request correlation middleware.
    /// </summary>
    public static IApplicationBuilder UseBicepExtensionMiddlewares(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseWhen(IsBicepExtensionRequest, branch =>
        {
            branch.UseBicepExtensionRequestCulture();
            branch.UseBicepExtensionRequestCorrelation();
        });

        return app;
    }

    private static bool IsBicepExtensionRequest(Microsoft.AspNetCore.Http.HttpContext context) =>
        context.Request.Path.Value?.Contains("/resource/", StringComparison.OrdinalIgnoreCase) == true ||
        context.Request.Path.Value?.Contains("/longRunningOperation/", StringComparison.OrdinalIgnoreCase) == true;
}
