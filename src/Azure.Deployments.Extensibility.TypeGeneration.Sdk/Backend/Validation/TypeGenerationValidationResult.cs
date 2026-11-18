// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;

internal sealed record TypeGenerationValidationResult
{
    public TypeGenerationValidationResult(TypeGenerationValidatedJsonPackage? model, ImmutableArray<TypeGenDiagnostic> diagnostics)
    {
        if (diagnostics.IsDefault || diagnostics.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException("Diagnostics must be initialized and cannot contain null entries.", nameof(diagnostics));
        }
        if (model is not null && diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            throw new ArgumentException("An error result cannot expose a validated package.", nameof(model));
        }

        Model = model;
        Diagnostics = diagnostics;
    }

    public TypeGenerationValidatedJsonPackage? Model { get; }

    public ImmutableArray<TypeGenDiagnostic> Diagnostics { get; }

    public bool Succeeded =>
        Model is not null &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
}

internal sealed class TypeGenerationValidatedJsonPackage
{
    public TypeGenerationValidatedJsonPackage(TypeGenerationSerializedPackage files)
    {
        ArgumentNullException.ThrowIfNull(files);
        Files = files;
    }

    public TypeGenerationSerializedPackage Files { get; }
}
