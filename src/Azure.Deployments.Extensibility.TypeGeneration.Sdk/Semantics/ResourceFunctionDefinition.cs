// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Semantics;

public sealed record ResourceFunctionDefinition
{
    public required string Name { get; init; }

    public TypeDefinition? InputType { get; init; }

    public required TypeDefinition OutputType { get; init; }

    public string? Description { get; init; }

    public VersionAvailability Availability { get; init; } = new();

    public SourceLocation? SourceLocation { get; init; }
}

public sealed record ProjectedResourceFunctionDefinition
{
    public required string Name { get; init; }

    public TypeDefinition? InputType { get; init; }

    public required TypeDefinition OutputType { get; init; }

    public string? Description { get; init; }

    public SourceLocation? SourceLocation { get; init; }
}
