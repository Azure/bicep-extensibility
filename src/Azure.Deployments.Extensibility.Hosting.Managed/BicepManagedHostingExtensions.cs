// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.Hosting.Managed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for configuring a managed Bicep extension host.
/// </summary>
public static class BicepManagedHostingExtensions
{
    /// <summary>
    /// Registers one exact-version managed Bicep extension and its required hosting services.
    /// </summary>
    public static WebApplicationBuilder AddBicepExtension(
        this WebApplicationBuilder builder,
        Action<IBicepExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var entryAssembly = Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("The managed Bicep extension host requires an entry assembly.");

        return AddBicepExtension(builder, configure, ManagedExtensionIdentityReader.Read(entryAssembly));
    }

    /// <summary>
    /// Adds the managed Bicep extension middleware, contract endpoints, and health endpoint.
    /// </summary>
    public static WebApplication UseBicepExtension(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var state = app.Services.GetService<ManagedApplicationState>()
            ?? throw new InvalidOperationException(
                $"Call {nameof(AddBicepExtension)} before calling {nameof(UseBicepExtension)}.");

        lock (state)
        {
            if (state.Integrated)
            {
                throw new InvalidOperationException(
                    $"{nameof(UseBicepExtension)} can only be called once for an application.");
            }

            state.Integrated = true;
        }

        app.UseBicepExtensionMiddlewares();
        app.MapBicepExtensionEndpoints();
        app.MapHealthChecks("/ping");

        return app;
    }

    internal static WebApplicationBuilder AddBicepExtension(
        WebApplicationBuilder builder,
        Action<IBicepExtensionBuilder> configure,
        BicepExtensionIdentity identity)
    {
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(ManagedApplicationState)))
        {
            throw new InvalidOperationException(
                $"{nameof(AddBicepExtension)} can only be called once for an application.");
        }

        builder.Services.AddBicepExtensionServices();
        builder.Services.AddHealthChecks();

        var registration = BicepExtensionRegistration.Create(builder.Services, configure);
        builder.Services.AddSingleton(identity);
        builder.Services.AddSingleton(registration);
        builder.Services.AddSingleton<IBicepExtensionResolver, ManagedBicepExtensionResolver>();
        builder.Services.AddSingleton<ManagedApplicationState>();

        return builder;
    }

    private sealed class ManagedApplicationState
    {
        public bool Integrated { get; set; }
    }
}
