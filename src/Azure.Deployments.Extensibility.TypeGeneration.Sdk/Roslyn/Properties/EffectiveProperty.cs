// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Properties;

internal sealed record EffectiveProperty(
    IPropertySymbol Symbol,
    string Name,
    string? Description,
    PropertyFlags Flags,
    bool IsSecure,
    SourceLocation? SourceLocation);
