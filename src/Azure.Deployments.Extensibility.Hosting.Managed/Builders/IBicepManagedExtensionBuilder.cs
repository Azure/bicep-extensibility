// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Hosting.Managed;

/// <summary>
/// Configures the handlers and behaviors for a managed Bicep extension.
/// </summary>
public interface IBicepManagedExtensionBuilder
{
    /// <summary>
    /// Registers a behavior that wraps every handler invocation, including unsupported versions.
    /// </summary>
    IBicepManagedExtensionBuilder AddGlobalHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class;

    /// <summary>
    /// Registers a global behavior using a factory invoked against the current request scope.
    /// </summary>
    IBicepManagedExtensionBuilder AddGlobalHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class;

    /// <summary>
    /// Registers a default handler by resolving it from the current request scope.
    /// </summary>
    IBicepManagedExtensionBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
        where THandler : class, IHandler;

    /// <summary>
    /// Registers a default handler using a factory invoked against the current request scope.
    /// </summary>
    IBicepManagedExtensionBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IHandler;

    /// <summary>
    /// Registers a behavior for this extension registration.
    /// </summary>
    IBicepManagedExtensionBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class;

    /// <summary>
    /// Registers a behavior factory for this extension registration.
    /// </summary>
    IBicepManagedExtensionBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class;

    /// <summary>
    /// Configures handlers and behaviors scoped to <paramref name="resourceType"/>.
    /// </summary>
    IBicepManagedExtensionBuilder ForResourceType(
        string resourceType,
        Action<IBicepResourceTypeBuilder> configure);
}
