// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Hosting.Managed;
using Azure.Deployments.Extensibility.Hosting.Managed.Resolution;
using Azure.Deployments.Extensibility.Hosting.Managed.Metadata;
using Azure.Deployments.Extensibility.Hosting.Managed.Validation;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering a managed Bicep extension.
/// </summary>
public static class BicepManagedServiceCollectionExtensions
{
    /// <summary>
    /// Registers the handlers, behaviors, and exact extension version for a managed Bicep extension.
    /// </summary>
    public static IServiceCollection AddBicepExtension(
        this IServiceCollection services,
        Action<IBicepManagedExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return AddBicepExtension(
            services,
            configure,
            BicepExtensionDescriptorReader.ReadEntryAssembly());
    }

    internal static IServiceCollection AddBicepExtension(
        this IServiceCollection services,
        Action<IBicepManagedExtensionBuilder> configure,
        BicepExtensionDescriptor descriptor)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(ManagedExtensionState)))
        {
            throw new InvalidOperationException(
                "AddBicepExtension can only be called once.");
        }

        services.AddBicepExtensionServices();
        services.AddHealthChecks();

        var registration = BicepExtensionRegistration.Create(
            services,
            extensionBuilder => configure(
                new BicepManagedExtensionBuilder(services, extensionBuilder)));
        var state = new ManagedExtensionState(descriptor.Version);

        services.AddSingleton(state);
        services.AddSingleton<IBicepExtensionResolver>(
            new ExactVersionExtensionResolver(descriptor.Version, registration));
        services.AddSingleton<IHostedService, ManagedExtensionStartupValidator>();

        return services;
    }
}
