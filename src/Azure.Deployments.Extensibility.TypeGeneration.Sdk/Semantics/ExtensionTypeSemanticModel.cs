// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.TypeGeneration.Semantics;

public sealed record ExtensionTypeSemanticModel
{
    public required string Name { get; init; }

    public required string Version { get; init; }

    public bool IsSingleton { get; init; }

    public bool IsPreview { get; init; }

    public bool IsDeprecated { get; init; }

    public TypeDefinition? Configuration { get; init; }

    public ResourceTypeDefinition? FallbackResourceType { get; init; }

    public required ImmutableArray<ResourceTypeDefinition> ResourceTypes { get; init; }

    public SourceLocation? SourceLocation { get; init; }
}
