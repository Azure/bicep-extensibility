// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Builders;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for enabling the development-time Scalar API explorer on a standard ASP.NET Core app.
/// </summary>
public static class WebApplicationScalarApiExplorerExtensions
{
    /// <summary>
    /// Enables the development-time Scalar API explorer with optional configuration.
    /// </summary>
    public static WebApplication EnableDevelopmentScalarApiExplorer(
        this WebApplication app,
        Action<ScalarApiExplorerBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var builder = new ScalarApiExplorerBuilder();
        configure?.Invoke(builder);

        app.MapBicepScalarApiExplorer(
            builder.ExamplesConfigurator,
            builder.Title,
            builder.ExtensionVersions);

        return app;
    }
}
