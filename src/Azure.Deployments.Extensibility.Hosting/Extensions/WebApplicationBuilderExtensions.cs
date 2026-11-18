// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Configures the Hosting SDK Bicep extension host on a standard ASP.NET Core builder.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a single immutable extension registration and the exact-version resolver for it.
    /// </summary>
    public static WebApplicationBuilder AddBicepExtension(
        this WebApplicationBuilder builder,
        string extensionVersion,
        Action<IBicepExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionVersion);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddBicepExtensionServices();
        builder.Services.AddHealthChecks();

        var registration = BicepExtensionRegistration.Create(builder.Services, configure);
        builder.Services.AddSingleton<IBicepExtensionResolver>(new ExactVersionBicepExtensionResolver(extensionVersion, registration));
        builder.Services.AddSingleton(new Azure.Deployments.Extensibility.Hosting.HostingBicepExtensionConfiguration(
            extensionVersion,
            GetExtensionIdentity()));

        return builder;
    }

    private static string GetExtensionIdentity()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "BicepExtensionIdentity", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(metadata?.Value))
        {
            throw new InvalidOperationException(
                "The Hosting SDK requires assembly metadata named 'BicepExtensionIdentity'. Add <AssemblyMetadata Include=\"BicepExtensionIdentity\" Value=\"...\" /> to your project file.");
        }

        return metadata.Value;
    }
}
