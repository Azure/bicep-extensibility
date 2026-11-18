// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend;

public sealed record TypeGenerationPackageResult
{
    public TypeGenerationPackageResult(
        TypeGenerationPackage? package,
        ImmutableArray<TypeGenDiagnostic> diagnostics)
    {
        if (diagnostics.IsDefault || diagnostics.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException("Diagnostics must be initialized and cannot contain null entries.", nameof(diagnostics));
        }

        if (package is not null && diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            throw new ArgumentException("An error result cannot expose a partial package.", nameof(package));
        }

        Package = package;
        Diagnostics = diagnostics;
    }

    public TypeGenerationPackage? Package { get; }

    public ImmutableArray<TypeGenDiagnostic> Diagnostics { get; }

    public bool Succeeded =>
        Package is not null &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
}
