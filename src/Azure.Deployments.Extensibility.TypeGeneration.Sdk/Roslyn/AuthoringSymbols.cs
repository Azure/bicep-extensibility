// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn;

internal sealed record AuthoringSymbols(
    INamedTypeSymbol ResourceType,
    INamedTypeSymbol TypeProperty,
    INamedTypeSymbol Configuration,
    INamedTypeSymbol PropertyFlags);
