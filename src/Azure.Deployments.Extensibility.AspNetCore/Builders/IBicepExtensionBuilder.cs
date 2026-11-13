// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// Configures a version-independent set of Bicep extension handlers and behaviors.
/// </summary>
/// <remarks>
/// This legacy hosting surface is kept for compatibility with existing consumers.
/// New applications should use the Azure.Deployments.Extensibility.Hosting package instead.
/// This interface is slated for removal in a future release.
/// </remarks>
public interface IBicepExtensionBuilder
{
    /// <summary>
    /// Registers a default handler by resolving it from the current request scope.
    /// </summary>
    IBicepExtensionBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
        where THandler : class, IHandler;

    /// <summary>
    /// Registers a default handler using a factory invoked against the current request scope.
    /// </summary>
    IBicepExtensionBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IHandler;

    /// <summary>
    /// Registers a behavior that applies to handlers in this extension registration.
    /// </summary>
    IBicepExtensionBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class;

    /// <summary>
    /// Registers a behavior factory invoked against the current request scope.
    /// </summary>
    IBicepExtensionBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class;

    /// <summary>
    /// Configures handlers and behaviors scoped to <paramref name="resourceType"/>.
    /// </summary>
    IBicepExtensionBuilder ForResourceType(
        string resourceType,
        Action<IBicepResourceTypeBuilder> configure);
}
