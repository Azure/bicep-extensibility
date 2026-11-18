// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Discovery;

internal sealed record ResourceRegistration(
    string Name,
    ImmutableArray<string> ApiVersions,
    INamedTypeSymbol Schema,
    SourceLocation? SourceLocation);

internal sealed record ConfigurationRegistration(INamedTypeSymbol Schema, SourceLocation? SourceLocation);
