// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// Configures pipeline behaviors for a specific handler registration.
/// The handler's <see cref="Type"/> is used as the key to scope behaviors.
/// </summary>
public sealed class HandlerPipelineBuilder(IServiceCollection services, HandlerPipelineRegistry registry, Type handlerType)
{
    /// <summary>
    /// Adds a pipeline behavior that will only execute when the associated handler is dispatched.
    /// Behaviors run in registration order, wrapping the inner handler.
    /// </summary>
    public HandlerPipelineBuilder AddPipelineBehavior<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        // Register the concrete type in DI (idempotent — safe if shared across handlers).
        services.TryAddScoped<TBehavior>();

        // Map this behavior to the specific handler type.
        registry.Add(handlerType, typeof(TBehavior));

        return this;
    }
}
