// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal static class ProjectGeneratorRunner
{
    public static async Task<(Compilation Compilation, ImmutableArray<TypeGenDiagnostic> Diagnostics)> CompileAsync(
        Project root,
        CancellationToken cancellationToken)
    {
        var graph = root.Solution.GetProjectDependencyGraph();
        var reachable = graph.GetProjectsThatThisProjectTransitivelyDependsOn(root.Id).Append(root.Id).ToHashSet();
        var projects = graph.GetTopologicallySortedProjects(cancellationToken).Where(reachable.Contains)
            .Select(id => root.Solution.GetProject(id)!).ToArray();
        var withoutGenerators = root.Solution;
        foreach (var project in projects)
        {
            withoutGenerators = withoutGenerators.WithProjectAnalyzerReferences(project.Id, []);
        }

        // Workspace must not run the generators first: its run diagnostics are not public.
        var replacements = new Dictionary<Compilation, Compilation>(ReferenceEqualityComparer.Instance);
        var diagnostics = ImmutableArray.CreateBuilder<TypeGenDiagnostic>();
        Compilation? rootCompilation = null;
        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var original = await withoutGenerators.GetProject(project.Id)!.GetCompilationAsync(cancellationToken)
                ?? throw new ToolException("TGH1004", $"Workspace did not produce a compilation for '{project.FilePath}'.");
            var compilation = original;
            foreach (var reference in original.References.OfType<CompilationReference>())
            {
                if (replacements.TryGetValue(reference.Compilation, out var generatedReference))
                {
                    compilation = compilation.ReplaceReference(reference,
                        generatedReference.ToMetadataReference(reference.Properties.Aliases, reference.Properties.EmbedInteropTypes));
                }
            }

            var generators = LoadGenerators(project, diagnostics, cancellationToken);
            if (!generators.IsEmpty)
            {
                if (compilation is not CSharpCompilation || project.ParseOptions is not CSharpParseOptions parseOptions)
                {
                    throw new ToolException("TGH1009",
                        $"Source generators for '{project.Language}' projects are not supported: '{project.FilePath}'.");
                }

                GeneratorDriver driver = CSharpGeneratorDriver.Create(
                    generators,
                    project.AnalyzerOptions.AdditionalFiles,
                    parseOptions,
                    project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
                driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _, cancellationToken);
                foreach (var result in driver.GetRunResult().Results)
                {
                    var generatorName = result.Generator.GetType().FullName ?? result.Generator.GetType().Name;
                    if (result.Exception is not null)
                    {
                        // A suppressed CS8784/CS8785 is still a failed generation, not a valid empty schema.
                        var detail = result.Diagnostics.IsEmpty
                            ? result.Exception.Message
                            : string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic =>
                                $"{diagnostic.Id}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}"));
                        diagnostics.Add(new("TGH1009", TypeGenDiagnosticSeverity.Error,
                            $"Source generator '{generatorName}' failed in '{project.FilePath}': {detail}", null));
                        continue;
                    }

                    foreach (var diagnostic in result.Diagnostics.Where(diagnostic =>
                        !diagnostic.IsSuppressed && diagnostic.Severity != DiagnosticSeverity.Hidden))
                    {
                        var location = diagnostic.Location;
                        diagnostics.Add(new("TGH1009", diagnostic.Severity switch
                        {
                            DiagnosticSeverity.Error => TypeGenDiagnosticSeverity.Error,
                            DiagnosticSeverity.Warning => TypeGenDiagnosticSeverity.Warning,
                            _ => TypeGenDiagnosticSeverity.Info,
                        }, $"Source generator in '{project.FilePath}': {diagnostic.Id}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}",
                            location.IsInSource && location.SourceTree is { } tree ? new SourceLocation
                            {
                                FilePath = tree.FilePath,
                                Start = location.SourceSpan.Start,
                                Length = location.SourceSpan.Length,
                            } : null));
                    }
                }
            }

            replacements.Add(original, compilation);
            if (project.Id == root.Id)
            {
                rootCompilation = compilation;
            }
        }

        return (rootCompilation ?? throw new ToolException("TGH1004", $"Root project '{root.FilePath}' was not compiled."),
            diagnostics.OrderBy(diagnostic => diagnostic.SourceLocation?.FilePath ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.SourceLocation?.Start ?? -1)
                .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                .ToImmutableArray());
    }

    private static ImmutableArray<ISourceGenerator> LoadGenerators(
        Project project,
        ImmutableArray<TypeGenDiagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        var generators = ImmutableArray.CreateBuilder<ISourceGenerator>();
        foreach (var reference in project.AnalyzerReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reference is UnresolvedAnalyzerReference)
            {
                diagnostics.Add(new("TGH1009", TypeGenDiagnosticSeverity.Error,
                    $"Cannot resolve analyzer/generator '{reference.FullPath}' for '{project.FilePath}'.", null));
                continue;
            }

            void OnLoadFailure(object? sender, AnalyzerLoadFailureEventArgs args) =>
                diagnostics.Add(new("TGH1009", TypeGenDiagnosticSeverity.Error,
                    $"Cannot load analyzer/generator '{reference.FullPath}' for '{project.FilePath}': {args.ErrorCode}: {args.Message}", null));

            var fileReference = reference as AnalyzerFileReference;
            if (fileReference is not null)
            {
                fileReference.AnalyzerLoadFailed += OnLoadFailure;
            }

            try
            {
                generators.AddRange(reference.GetGenerators(project.Language));
            }
            finally
            {
                if (fileReference is not null)
                {
                    fileReference.AnalyzerLoadFailed -= OnLoadFailure;
                }
            }
        }

        return generators.ToImmutable();
    }
}
