// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

public sealed record TypeGenDiagnostic(
    string Code,
    TypeGenDiagnosticSeverity Severity,
    string Message,
    SourceLocation? SourceLocation);

public enum TypeGenDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}
