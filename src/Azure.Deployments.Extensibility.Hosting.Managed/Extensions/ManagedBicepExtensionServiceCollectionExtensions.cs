// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Hosting.Managed;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Managed Bicep extension services in <see cref="IServiceCollection"/>.
/// </summary>
public static class ManagedBicepExtensionServiceCollectionExtensions
{
    /// <summary>
    /// Registers a single-version Managed Bicep extension using metadata read from the entry assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration delegate for extension handlers and behaviors.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBicepExtension(
        this IServiceCollection services,
        Action<IBicepExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var metadata = BicepExtensionMetadata.FromAssembly(assembly);

        return services.AddBicepExtension(metadata, configure);
    }

    /// <summary>
    /// Registers a single-version Managed Bicep extension using metadata read from the specified assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to read extension identity metadata from.</param>
    /// <param name="configure">The configuration delegate for extension handlers and behaviors.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBicepExtension(
        this IServiceCollection services,
        Assembly assembly,
        Action<IBicepExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(configure);

        var metadata = BicepExtensionMetadata.FromAssembly(assembly);
        return services.AddBicepExtension(metadata, configure);
    }

    /// <summary>
    /// Registers a single-version Managed Bicep extension with the provided metadata.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="metadata">The extension identity metadata.</param>
    /// <param name="configure">The configuration delegate for extension handlers and behaviors.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBicepExtension(
        this IServiceCollection services,
        BicepExtensionMetadata metadata,
        Action<IBicepExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(configure);

        if (services.Any(sd => sd.ServiceType == typeof(ManagedBicepExtensionMarker)))
        {
            throw new InvalidOperationException(
                "AddBicepExtension has already been called. Only one Bicep extension registration is supported per Managed host.");
        }

        services.AddSingleton(new ManagedBicepExtensionMarker());
        services.AddBicepExtensionServices();
        services.AddHealthChecks();
        services.AddSingleton(metadata);

        var registration = BicepExtensionRegistration.Create(services, configure);
        var resolver = new ExactVersionBicepExtensionResolver(metadata.Version, registration);
        services.AddSingleton<IBicepExtensionResolver>(resolver);

        return services;
    }
}

internal sealed class ManagedBicepExtensionMarker
{
}
