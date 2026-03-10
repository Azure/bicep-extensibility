// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Semver;

namespace Azure.Deployments.Extensibility.AspNetCore.Pipeline;

/// <summary>
/// Maps handler concrete types to their registered pipeline behavior types.
/// Populated at startup; read-only at dispatch time.
/// </summary>
public sealed class HandlerPipelineRegistry
{
    private readonly Dictionary<Type, List<Type>> behaviorsByHandlerType = [];
    private readonly Dictionary<SemVersionRange, List<Type>> behaviorsByVersionRange = [];
    private readonly List<Type> globalBehaviorTypes = [];

    /// <summary>
    /// Records that <paramref name="behaviorType"/> should run in the pipeline
    /// for the handler identified by <paramref name="handlerType"/>.
    /// </summary>
    public void Add(Type handlerType, Type behaviorType)
    {
        if (!this.behaviorsByHandlerType.TryGetValue(handlerType, out var types))
        {
            types = [];
            this.behaviorsByHandlerType[handlerType] = types;
        }

        types.Add(behaviorType);
    }

    /// <summary>
    /// Records that <paramref name="behaviorType"/> should run in the pipeline
    /// for all handlers registered under <paramref name="versionRange"/>.
    /// Version-scoped behaviors run after global behaviors but before handler-specific behaviors.
    /// </summary>
    public void AddVersionScoped(SemVersionRange versionRange, Type behaviorType)
    {
        if (!this.behaviorsByVersionRange.TryGetValue(versionRange, out var types))
        {
            types = [];
            this.behaviorsByVersionRange[versionRange] = types;
        }

        types.Add(behaviorType);
    }

    /// <summary>
    /// Records that <paramref name="behaviorType"/> should run in the pipeline
    /// for all handlers. Global behaviors are outermost in the pipeline.
    /// </summary>
    public void AddGlobal(Type behaviorType)
    {
        this.globalBehaviorTypes.Add(behaviorType);
    }

    /// <summary>
    /// Resolves pipeline behaviors for the given handler type, filtered to those
    /// that implement <see cref="IHandlerPipelineBehavior{TRequest, TResponse}"/>
    /// matching the requested request/response types.
    /// Global behaviors come first (outermost), followed by version-scoped behaviors,
    /// then handler-specific behaviors.
    /// </summary>
    public IReadOnlyList<IHandlerPipelineBehavior<TRequest, TResponse>> Resolve<TRequest, TResponse>(
        Type handlerType,
        SemVersionRange? versionRange,
        IServiceProvider serviceProvider)
    {
        var allBehaviorTypes = new List<Type>(this.globalBehaviorTypes);

        if (versionRange is not null && this.behaviorsByVersionRange.TryGetValue(versionRange, out var versionScoped))
        {
            allBehaviorTypes.AddRange(versionScoped);
        }

        if (this.behaviorsByHandlerType.TryGetValue(handlerType, out var handlerSpecific))
        {
            allBehaviorTypes.AddRange(handlerSpecific);
        }

        if (allBehaviorTypes.Count == 0)
        {
            return [];
        }

        var targetInterface = typeof(IHandlerPipelineBehavior<TRequest, TResponse>);

        return allBehaviorTypes
            .Where(targetInterface.IsAssignableFrom)
            .Select(type => (IHandlerPipelineBehavior<TRequest, TResponse>)serviceProvider.GetRequiredService(type))
            .ToArray();
    }
}
