// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for configuring the Managed Bicep extension HTTP pipeline and endpoints on <see cref="WebApplication"/>.
/// </summary>
public static class ManagedBicepExtensionWebApplicationExtensions
{
    private const string AggregateAppliedPropertyKey = "__BicepExtensionManagedAggregateApplied";

    /// <summary>
    /// Configures the Managed Bicep extension middleware pipeline, health check endpoint (<c>GET /ping</c>),
    /// and bare Bicep extension contract routes.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same web application for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>AddBicepExtension</c> has not been called, or when <c>UseBicepExtension</c> is called more than once.
    /// </exception>
    public static WebApplication UseBicepExtension(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var appBuilder = (IApplicationBuilder)app;

        if (appBuilder.Properties.ContainsKey(AggregateAppliedPropertyKey))
        {
            throw new InvalidOperationException(
                "UseBicepExtension has already been called on this application.");
        }

        var resolver = app.Services.GetService<IBicepExtensionResolver>();
        if (resolver is null)
        {
            throw new InvalidOperationException(
                "Call AddBicepExtension before calling UseBicepExtension.");
        }

        appBuilder.Properties[AggregateAppliedPropertyKey] = true;

        app.UseBicepExtensionMiddlewares();
        app.MapHealthChecks("/ping");
        app.MapBicepExtensionEndpoints();

        return app;
    }
}
