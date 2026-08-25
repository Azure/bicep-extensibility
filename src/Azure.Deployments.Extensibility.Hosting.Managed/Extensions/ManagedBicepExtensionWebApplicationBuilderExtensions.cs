// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Hosting.Managed;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for configuring Managed Bicep extensions on <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class ManagedBicepExtensionWebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a single-version Managed Bicep extension using metadata read from the entry assembly.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configure">The configuration delegate for extension handlers and behaviors.</param>
    /// <returns>The same web application builder for chaining.</returns>
    public static WebApplicationBuilder AddBicepExtension(
        this WebApplicationBuilder builder,
        Action<IBicepExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var metadata = BicepExtensionMetadata.FromAssembly(assembly);

        builder.Services.AddBicepExtension(metadata, configure);
        return builder;
    }

    /// <summary>
    /// Registers a single-version Managed Bicep extension using metadata read from the specified assembly.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="assembly">The assembly to read extension identity metadata from.</param>
    /// <param name="configure">The configuration delegate for extension handlers and behaviors.</param>
    /// <returns>The same web application builder for chaining.</returns>
    public static WebApplicationBuilder AddBicepExtension(
        this WebApplicationBuilder builder,
        Assembly assembly,
        Action<IBicepExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(configure);

        var metadata = BicepExtensionMetadata.FromAssembly(assembly);
        builder.Services.AddBicepExtension(metadata, configure);
        return builder;
    }

    /// <summary>
    /// Registers a single-version Managed Bicep extension with the provided metadata.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="metadata">The extension identity metadata.</param>
    /// <param name="configure">The configuration delegate for extension handlers and behaviors.</param>
    /// <returns>The same web application builder for chaining.</returns>
    public static WebApplicationBuilder AddBicepExtension(
        this WebApplicationBuilder builder,
        BicepExtensionMetadata metadata,
        Action<IBicepExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddBicepExtension(metadata, configure);
        return builder;
    }
}
