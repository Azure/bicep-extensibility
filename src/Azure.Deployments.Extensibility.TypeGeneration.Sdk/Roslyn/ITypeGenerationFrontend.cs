// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn;

public interface ITypeGenerationFrontend
{
    TypeGenerationResult Build(
        Compilation compilation,
        ExtensionBuildMetadata metadata,
        CancellationToken cancellationToken = default);
}
