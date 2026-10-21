// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Microsoft.Build.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal static class ProjectCompilationLoader
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<ProjectAnalysis> AnalyzeAsync(ToolOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var evaluated = ProjectMetadataReader.Read(options, cancellationToken);
            using var workspace = MSBuildWorkspace.Create(evaluated.GlobalProperties);
            workspace.LoadMetadataForReferencedProjects = false;
            workspace.SkipUnrecognizedProjects = false;
            var workspaceDiagnostics = new ConcurrentQueue<WorkspaceDiagnostic>();
            using var registration = workspace.RegisterWorkspaceFailedHandler(args => workspaceDiagnostics.Enqueue(args.Diagnostic));
            await workspace.OpenProjectAsync(options.ProjectPath, cancellationToken: cancellationToken);
            var candidates = workspace.CurrentSolution.Projects.Where(project =>
                string.Equals(project.FilePath, options.ProjectPath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(project.OutputFilePath, evaluated.OutputPath, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (candidates.Length != 1)
            {
                throw new ToolException("TGH1003",
                    $"Cannot uniquely select framework '{evaluated.Framework}' for '{options.ProjectPath}' " +
                    $"using its evaluated output path '{evaluated.OutputPath}'. Keep target framework output paths distinct.");
            }

            var project = candidates[0];
            var compilation = await project.GetCompilationAsync(cancellationToken);
            var loadDiagnostics = workspaceDiagnostics.Concat(workspace.Diagnostics)
                .OrderBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                .Select(diagnostic => new TypeGenDiagnostic("TGH1004",
                    diagnostic.Kind == WorkspaceDiagnosticKind.Failure ? TypeGenDiagnosticSeverity.Error : TypeGenDiagnosticSeverity.Warning,
                    $"Project '{options.ProjectPath}': {diagnostic.Message}", null))
                .Distinct()
                .ToImmutableArray();
            if (loadDiagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
            {
                return new(new(null, loadDiagnostics), loadDiagnostics.Select(FormatWithoutLocation).ToImmutableArray());
            }

            if (compilation is null)
            {
                throw new ToolException("TGH1004", $"Workspace did not produce a compilation for '{options.ProjectPath}'.");
            }

            var result = new TypeGenerationFrontend().Build(compilation, evaluated.Metadata, cancellationToken);
            result = new(result.Model, loadDiagnostics.AddRange(result.Diagnostics));
            var messages = ImmutableArray.CreateBuilder<string>();
            foreach (var diagnostic in result.Diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var message = FormatWithoutLocation(diagnostic);
                if (diagnostic.SourceLocation is { } location)
                {
                    var documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(location.FilePath).FirstOrDefault();
                    var document = documentId is null ? null : workspace.CurrentSolution.GetDocument(documentId);
                    if (document is not null)
                    {
                        var text = await document.GetTextAsync(cancellationToken);
                        var position = text.Lines.GetLinePosition(location.Start);
                        message = $"{location.FilePath}({position.Line + 1},{position.Character + 1}): {message}";
                    }
                    else
                    {
                        message = $"{location.FilePath}[{location.Start}+{location.Length}]: {message}";
                    }
                }

                messages.Add(message);
            }

            return new(result, messages.ToImmutable());
        }
        catch (InvalidProjectFileException exception)
        {
            throw new ToolException("TGH1002", $"Cannot evaluate '{options.ProjectPath}': {exception.Message}", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new ToolException("TGH1004", $"Cannot load '{options.ProjectPath}': {exception.Message}", exception);
        }
        catch (IOException exception)
        {
            throw new ToolException("TGH1002", $"Cannot read project '{options.ProjectPath}': {exception.Message}", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new ToolException("TGH1002", $"Cannot access project '{options.ProjectPath}': {exception.Message}", exception);
        }
    }

    private static string FormatWithoutLocation(TypeGenDiagnostic diagnostic) =>
        $"{diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Code}: {diagnostic.Message}";
}
