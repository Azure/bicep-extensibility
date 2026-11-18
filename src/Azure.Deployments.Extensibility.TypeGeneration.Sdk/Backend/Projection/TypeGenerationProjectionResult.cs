// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;

internal sealed record TypeGenerationProjectionResult
{
    public TypeGenerationProjectionResult(
        TypeGenerationProjectedExtension? model,
        ImmutableArray<TypeGenDiagnostic> diagnostics)
    {
        if (diagnostics.IsDefault || diagnostics.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException("Diagnostics must be initialized and cannot contain null entries.", nameof(diagnostics));
        }

        if (model is not null && diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            throw new ArgumentException("An error result cannot expose a partial projection.", nameof(model));
        }

        Model = model;
        Diagnostics = diagnostics;
    }

    public TypeGenerationProjectedExtension? Model { get; }

    public ImmutableArray<TypeGenDiagnostic> Diagnostics { get; }

    public bool Succeeded =>
        Model is not null &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
}

internal sealed class TypeGenerationProjectedExtension
{
    public TypeGenerationProjectedExtension(
        ExtensionTypeSemanticModel source,
        ImmutableArray<ProjectedResourceTypeDefinition> resourceTypes)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (resourceTypes.IsDefault || resourceTypes.Any(resource => resource is null))
        {
            throw new ArgumentException("Projected resources must be initialized and cannot contain null entries.", nameof(resourceTypes));
        }

        Name = source.Name;
        Version = source.Version;
        IsSingleton = source.IsSingleton;
        Configuration = source.Configuration;
        SourceLocation = source.SourceLocation;
        ResourceTypes = resourceTypes;
    }

    public string Name { get; }

    public string Version { get; }

    public bool IsSingleton { get; }

    public TypeDefinition? Configuration { get; }

    public ImmutableArray<ProjectedResourceTypeDefinition> ResourceTypes { get; }

    public SourceLocation? SourceLocation { get; }
}
