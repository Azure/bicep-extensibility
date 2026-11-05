// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn;

internal static class SourceLocationConverter
{
    public static SourceLocation? FromSymbol(ISymbol symbol)
        => symbol.Locations.Where(location => location.IsInSource)
            .OrderBy(location => location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(location => location.SourceSpan.Start)
            .Select(FromLocation)
            .FirstOrDefault();

    public static SourceLocation? FromLocation(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (!location.IsInSource || location.SourceTree is not { } tree)
        {
            return null;
        }

        return new SourceLocation
        {
            FilePath = tree.FilePath,
            Start = location.SourceSpan.Start,
            Length = location.SourceSpan.Length,
        };
    }
}
