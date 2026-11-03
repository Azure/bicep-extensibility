// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal sealed class ProjectDiagnosticFormatter
{
    private readonly ImmutableDictionary<string, SourceText?> sources;

    private ProjectDiagnosticFormatter(ImmutableDictionary<string, SourceText?> sources) => this.sources = sources;

    public IEnumerable<string> SourcePaths => sources.Keys;

    public static async Task<ProjectDiagnosticFormatter> CreateAsync(
        Compilation compilation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        var sources = ImmutableDictionary.CreateBuilder<string, SourceText?>(StringComparer.Ordinal);
        var visited = new HashSet<Compilation>(ReferenceEqualityComparer.Instance);
        var pending = new Queue<Compilation>();
        pending.Enqueue(compilation);
        while (pending.TryDequeue(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!visited.Add(current)) { continue; }
            foreach (var tree in current.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = await tree.GetTextAsync(cancellationToken);
                if (!sources.TryGetValue(tree.FilePath, out var previous))
                {
                    sources.Add(tree.FilePath, text);
                }
                else if (previous is not null && !previous.ContentEquals(text))
                {
                    sources[tree.FilePath] = null;
                }
            }
            foreach (var reference in current.References.OfType<CompilationReference>())
            {
                pending.Enqueue(reference.Compilation);
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new(sources.ToImmutable());
    }

    public ImmutableArray<string> Format(
        IEnumerable<TypeGenDiagnostic> diagnostics,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var messages = ImmutableArray.CreateBuilder<string>();
        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            messages.Add(Format(diagnostic));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return messages.ToImmutable();
    }

    public string Format(TypeGenDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        var message = $"{diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Code}: {diagnostic.Message}";
        if (diagnostic.SourceLocation is not { } location)
        {
            return message;
        }
        if (location.FilePath is { } path && sources.TryGetValue(path, out var text) && text is not null &&
            location.Start >= 0 && location.Length >= 0 && location.Start <= text.Length &&
            location.Length <= text.Length - location.Start)
        {
            var position = text.Lines.GetLinePosition(location.Start);
            return $"{location.FilePath}({position.Line + 1},{position.Character + 1}): {message}";
        }
        return $"{location.FilePath}[{location.Start}+{location.Length}]: {message}";
    }
}
