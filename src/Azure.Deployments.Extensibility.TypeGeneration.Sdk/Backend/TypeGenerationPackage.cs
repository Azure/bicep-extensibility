// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend;

public sealed class TypeGenerationPackage
{
    internal TypeGenerationPackage(
        ImmutableArray<byte> indexJson,
        ImmutableArray<byte> typesJson,
        ImmutableArray<byte> archiveBytes)
    {
        RequireContent(indexJson, nameof(indexJson));
        RequireContent(typesJson, nameof(typesJson));
        RequireContent(archiveBytes, nameof(archiveBytes));
        IndexJson = indexJson;
        TypesJson = typesJson;
        ArchiveBytes = archiveBytes;
    }

    public ImmutableArray<byte> IndexJson { get; }

    public ImmutableArray<byte> TypesJson { get; }

    public ImmutableArray<byte> ArchiveBytes { get; }

    private static void RequireContent(ImmutableArray<byte> content, string parameterName)
    {
        if (content.IsDefaultOrEmpty)
        {
            throw new ArgumentException("Package content must be initialized and nonempty.", parameterName);
        }
    }
}
