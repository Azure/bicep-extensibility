// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Semantics;

public sealed record PropertyDefinition
{
    public required string Name { get; init; }

    public required TypeDefinition Type { get; init; }

    public PropertyFlags Flags { get; init; }

    public string? Description { get; init; }

    public PropertyVersioning Versioning { get; init; } = new();

    public SourceLocation? SourceLocation { get; init; }
}

[Flags]
public enum PropertyFlags
{
    None = 0,
    Required = 1 << 0,
    ReadOnly = 1 << 1,
    WriteOnly = 1 << 2,
    DeployTimeConstant = 1 << 3,
    Identifier = 1 << 4,
}
