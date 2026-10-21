// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.Build.Locator;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal sealed record ProjectAnalysis(TypeGenerationResult Result, ImmutableArray<string> DiagnosticMessages);

internal static class ProjectAnalyzer
{
    private static readonly object RegistrationLock = new();

    public static Task<ProjectAnalysis> AnalyzeAsync(ToolOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string path;
        try
        {
            path = Path.GetFullPath(options.ProjectPath);
        }
        catch (ArgumentException exception)
        {
            throw new ToolException("TGH1002", $"Invalid project path '{options.ProjectPath}': {exception.Message}", exception);
        }

        if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            throw new ToolException("TGH1002", $"A C# project file is required: '{path}'.");
        }

        lock (RegistrationLock)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                try
                {
                    MSBuildLocator.RegisterDefaults();
                }
                catch (InvalidOperationException exception)
                {
                    throw new ToolException("TGH1001", $"Cannot register MSBuild: {exception.Message}", exception);
                }
            }
        }

        // The callee is not inlined: its MSBuild types must load only after registration.
        return ProjectCompilationLoader.AnalyzeAsync(options with { ProjectPath = path }, cancellationToken);
    }
}
