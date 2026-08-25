// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// Configures handlers and behaviors for one Bicep resource type.
/// </summary>
/// <remarks>
/// This legacy hosting surface is kept for compatibility with existing consumers.
/// New applications should use the Azure.Deployments.Extensibility.Hosting package instead.
/// This interface is slated for removal in a future release.
/// </remarks>
public interface IBicepResourceTypeBuilder
{
    /// <summary>
    /// Registers a handler by resolving it from the current request scope.
    /// </summary>
    IBicepResourceTypeBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
        where THandler : class, IHandler;

    /// <summary>
    /// Registers a handler using a factory invoked against the current request scope.
    /// </summary>
    IBicepResourceTypeBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IHandler;

    /// <summary>
    /// Registers a behavior that applies to this resource type.
    /// </summary>
    IBicepResourceTypeBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class;

    /// <summary>
    /// Registers a behavior factory invoked against the current request scope.
    /// </summary>
    IBicepResourceTypeBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class;
}
