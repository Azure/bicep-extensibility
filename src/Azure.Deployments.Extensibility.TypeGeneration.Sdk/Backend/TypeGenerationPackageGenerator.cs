// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Backend.Packaging;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend;

public sealed class TypeGenerationPackageGenerator
{
    public TypeGenerationPackageResult Generate(
        ExtensionTypeSemanticModel model,
        CancellationToken cancellationToken = default)
    {
        var json = TypeGenerationJsonGenerator.Generate(model, cancellationToken);
        if (!json.Succeeded)
        {
            return new(null, json.Diagnostics);
        }

        var archive = TypeGenerationArchiveWriter.Write(json.Model!, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return new(archive.Package, TypeGenerationModelValidator.OrderDiagnostics(json.Diagnostics.AddRange(archive.Diagnostics)));
    }
}
