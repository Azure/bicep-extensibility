// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions;

/// <summary>
/// Extension methods for mapping Bicep extension endpoints.
/// </summary>
public static class BicepExtensionEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the resource preview, create-or-update, get, and delete endpoints.
    /// </summary>
    public static IEndpointConventionBuilder MapResourceActions(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ValidateRuntime(endpoints);

        var group = endpoints.MapGroup(string.Empty);
        MapResourceActionEndpoints(group);

        return group;
    }

    /// <summary>
    /// Maps the long-running operation status endpoint.
    /// </summary>
    public static IEndpointConventionBuilder MapLongRunningOperationActions(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ValidateRuntime(endpoints);

        return endpoints.MapPost(
            "/{extensionVersion}/longRunningOperation/get",
            BicepExtensionHandlerDispatcher.DispatchLongRunningOperationGetAsync);
    }

    /// <summary>
    /// Maps all Bicep extension contract endpoints.
    /// </summary>
    public static IEndpointConventionBuilder MapBicepExtensionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ValidateRuntime(endpoints);

        var group = endpoints.MapGroup(string.Empty);
        MapResourceActionEndpoints(group);
        group.MapPost(
            "/{extensionVersion}/longRunningOperation/get",
            BicepExtensionHandlerDispatcher.DispatchLongRunningOperationGetAsync);

        return group;
    }

    private static void MapResourceActionEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/{extensionVersion}/resource/preview",
            BicepExtensionHandlerDispatcher.DispatchResourcePreviewAsync);
        endpoints.MapPost(
            "/{extensionVersion}/resource/createOrUpdate",
            BicepExtensionHandlerDispatcher.DispatchResourceCreateOrUpdateAsync);
        endpoints.MapPost(
            "/{extensionVersion}/resource/get",
            BicepExtensionHandlerDispatcher.DispatchResourceGetAsync);
        endpoints.MapPost(
            "/{extensionVersion}/resource/delete",
            BicepExtensionHandlerDispatcher.DispatchResourceDeleteAsync);
    }

    private static void ValidateRuntime(IEndpointRouteBuilder endpoints)
    {
        var resolvers = endpoints.ServiceProvider.GetServices<IBicepExtensionResolver>().Take(2).ToArray();

        if (resolvers.Length != 1)
        {
            throw new InvalidOperationException(
                $"Exactly one {nameof(IBicepExtensionResolver)} must be registered before mapping Bicep extension endpoints.");
        }

        var serviceProviderIsService = endpoints.ServiceProvider.GetService<IServiceProviderIsService>();

        if (serviceProviderIsService?.IsService(typeof(HandlerInvoker)) != true)
        {
            throw new InvalidOperationException(
                $"Call {nameof(BicepExtensionServiceCollectionExtensions.AddBicepExtensionHandlerRuntime)} before mapping Bicep extension endpoints.");
        }
    }
}
