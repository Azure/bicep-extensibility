// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Decorators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// Configures handler decorators for a specific handler registration.
/// The handler's <see cref="Type"/> is used as the key to scope decorators.
/// </summary>
public sealed class HandlerDecoratorBuilder(IServiceCollection services, HandlerDecoratorRegistry registry, Type handlerType)
{
    /// <summary>
    /// Adds a decorator that will only execute when the associated handler is dispatched.
    /// Decorators run in registration order, wrapping the inner handler.
    /// </summary>
    public HandlerDecoratorBuilder AddHandlerDecorator<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TDecorator>()
        where TDecorator : class
    {
        // Register the concrete type in DI (idempotent — safe if shared across handlers).
        services.TryAddScoped<TDecorator>();

        // Map this decorator to the specific handler type.
        registry.AddHandlerScoped(handlerType, typeof(TDecorator));

        return this;
    }
}
