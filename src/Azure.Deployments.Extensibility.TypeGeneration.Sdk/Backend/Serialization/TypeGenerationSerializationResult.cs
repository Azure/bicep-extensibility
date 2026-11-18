// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Lowering;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;

internal sealed record TypeGenerationSerializationResult
{
    public TypeGenerationSerializationResult(TypeGenerationSerializedPackage? model, ImmutableArray<TypeGenDiagnostic> diagnostics)
    {
        if (diagnostics.IsDefault || diagnostics.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException("Diagnostics must be initialized and cannot contain null entries.", nameof(diagnostics));
        }

        if (model is not null && diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            throw new ArgumentException("An error result cannot expose partial serialized output.", nameof(model));
        }

        Model = model;
        Diagnostics = diagnostics;
    }

    public TypeGenerationSerializedPackage? Model { get; }

    public ImmutableArray<TypeGenDiagnostic> Diagnostics { get; }

    public bool Succeeded =>
        Model is not null &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
}

internal sealed class TypeGenerationSerializedPackage
{
    public const string IndexFileName = "index.json";

    public const string TypesFileName = TypeGenerationLoweredPackage.TypesFileName;

    public TypeGenerationSerializedPackage(ImmutableArray<byte> indexJson, ImmutableArray<byte> typesJson)
    {
        if (indexJson.IsDefaultOrEmpty)
        {
            throw new ArgumentException("Index JSON must be initialized and nonempty.", nameof(indexJson));
        }
        if (typesJson.IsDefaultOrEmpty)
        {
            throw new ArgumentException("Type JSON must be initialized and nonempty.", nameof(typesJson));
        }

        IndexJson = indexJson;
        TypesJson = typesJson;
    }

    public ImmutableArray<byte> IndexJson { get; }

    public ImmutableArray<byte> TypesJson { get; }
}
