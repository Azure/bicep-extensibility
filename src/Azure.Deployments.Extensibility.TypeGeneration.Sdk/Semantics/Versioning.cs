// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Semantics;

public sealed record VersionAvailability
{
    public string? AddedAt { get; init; }

    public string? RemovedAt { get; init; }
}

public sealed record VersionTransition<T>
{
    public required string AtVersion { get; init; }

    public required T PreviousValue { get; init; }
}

public sealed record PropertyVersioning
{
    public VersionAvailability Availability { get; init; } = new();

    public VersionTransition<string>? NameChangedFrom { get; init; }

    public VersionTransition<TypeDefinition>? TypeChangedFrom { get; init; }

    public VersionTransition<bool>? RequiredChangedFrom { get; init; }
}
