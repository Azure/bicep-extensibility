// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration;

public sealed record TypeGenerationResult
{
    public TypeGenerationResult(
        ExtensionTypeSemanticModel? model,
        ImmutableArray<TypeGenDiagnostic> diagnostics)
    {
        if (diagnostics.IsDefault || diagnostics.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException("Diagnostics must be initialized and cannot contain null entries.", nameof(diagnostics));
        }

        if (model is not null && diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            throw new ArgumentException("An error result cannot expose a partial model.", nameof(model));
        }

        Model = model;
        Diagnostics = diagnostics;
    }

    public ExtensionTypeSemanticModel? Model { get; }

    public ImmutableArray<TypeGenDiagnostic> Diagnostics { get; }

    public bool Succeeded =>
        Model is not null &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
}
