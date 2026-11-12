// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Lowering;

internal sealed record TypeGenerationLoweringResult
{
    public TypeGenerationLoweringResult(TypeGenerationLoweredPackage? model, ImmutableArray<TypeGenDiagnostic> diagnostics)
    {
        if (diagnostics.IsDefault || diagnostics.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException("Diagnostics must be initialized and cannot contain null entries.", nameof(diagnostics));
        }

        if (model is not null && diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            throw new ArgumentException("An error result cannot expose a partial lowered graph.", nameof(model));
        }

        Model = model;
        Diagnostics = diagnostics;
    }

    public TypeGenerationLoweredPackage? Model { get; }

    public ImmutableArray<TypeGenDiagnostic> Diagnostics { get; }

    public bool Succeeded =>
        Model is not null &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
}

internal sealed class TypeGenerationLoweredPackage
{
    public const string TypesFileName = "types.json";

    public TypeGenerationLoweredPackage(TypeIndex index, ImmutableArray<TypeBase> types)
    {
        ArgumentNullException.ThrowIfNull(index);
        if (types.IsDefault || types.Any(type => type is null))
        {
            throw new ArgumentException("Lowered types must be initialized and cannot contain null entries.", nameof(types));
        }

        Index = index;
        Types = types;
    }

    public TypeIndex Index { get; }

    public ImmutableArray<TypeBase> Types { get; }
}
