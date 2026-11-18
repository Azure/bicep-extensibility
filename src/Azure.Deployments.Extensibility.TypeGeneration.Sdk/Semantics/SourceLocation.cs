// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Semantics;

public sealed record SourceLocation
{
    public required string FilePath { get; init; }

    public required int Start { get; init; }

    public required int Length { get; init; }
}
