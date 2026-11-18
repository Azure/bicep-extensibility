// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Wires the shared Bicep extension middleware, endpoints, health checks, and /ping route onto a real app.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the Hosting SDK middleware and routes.
    /// </summary>
    public static WebApplication UseBicepExtension(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var resolvers = app.Services.GetServices<IBicepExtensionResolver>().ToArray();
        if (resolvers.Length != 1)
        {
            throw new InvalidOperationException(
                $"The Hosting SDK expects exactly one '{nameof(IBicepExtensionResolver)}' registration, but found {resolvers.Length}.");
        }

        var configuration = app.Services.GetService<Azure.Deployments.Extensibility.Hosting.HostingBicepExtensionConfiguration>();
        if (configuration is null)
        {
            throw new InvalidOperationException(
                "The Hosting SDK must be configured with AddBicepExtension before calling UseBicepExtension.");
        }

        if (configuration.AggregationConfigured)
        {
            throw new InvalidOperationException("The Hosting SDK has already been configured.");
        }

        configuration.AggregationConfigured = true;
        app.UseBicepExtensionMiddlewares();
        app.MapBicepExtensionEndpoints();
        app.MapGet("/ping", () => Results.Text("pong"));
        app.MapHealthChecks("/healthz");

        return app;
    }
}
