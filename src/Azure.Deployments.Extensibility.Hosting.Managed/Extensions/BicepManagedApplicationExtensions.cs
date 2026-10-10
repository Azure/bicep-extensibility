// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.AspNetCore.Builders;
using Azure.Deployments.Extensibility.Hosting.Managed.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Extensions;

/// <summary>
/// Extension methods for configuring a managed Bicep extension application.
/// </summary>
public static class BicepManagedApplicationExtensions
{
    /// <summary>
    /// Maps the API explorer and OpenAPI document in the Development environment.
    /// </summary>
    public static WebApplication MapManagedScalarApiExplorer(
        this WebApplication app,
        Action<ScalarApiExplorerBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var state = app.Services.GetRequiredService<ManagedExtensionState>();

        return app.MapBicepScalarApiExplorer(
            builder =>
            {
                configure?.Invoke(builder);
                builder.WithExtensionVersions(state.ExtensionVersion);
            });
    }

    /// <summary>
    /// Maps the API explorer and OpenAPI document in the Development environment.
    /// </summary>
    public static WebApplication MapDevelopmentApiExplorer(
        this WebApplication app,
        Action<ScalarApiExplorerBuilder>? configure = null) =>
        app.MapManagedScalarApiExplorer(configure);

    /// <summary>
    /// Installs the Bicep extension middleware and maps the contract and health endpoints.
    /// </summary>
    public static WebApplication MapBicepExtension(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var state = app.Services.GetRequiredService<ManagedExtensionState>();
        state.MarkApplicationConfigured();

        app.UseBicepExtensionMiddlewares();
        app.MapBicepExtensionEndpoints();
        app.MapHealthChecks("/ping")
            .WithMetadata(new HttpMethodMetadata([HttpMethods.Get]));

        return app;
    }

    /// <summary>
    /// Installs the Bicep extension middleware and maps the contract and health endpoints.
    /// </summary>
    public static WebApplication UseBicepExtension(this WebApplication app) =>
        app.MapBicepExtension();
}
