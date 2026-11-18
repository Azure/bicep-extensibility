// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal static class TypeGenerationOutputPath
{
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static string Resolve(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw Invalid("An output file path is required.");
        }
        try
        {
            if (OperatingSystem.IsWindows() && Components(outputPath).Any(component =>
                component is not ("." or "..") && (component.EndsWith(' ') || component.EndsWith('.'))))
            {
                throw Invalid($"Output '{outputPath}' contains an ambiguous trailing-dot/space component.");
            }
            var fullPath = Path.GetFullPath(outputPath);
            if (Path.EndsInDirectorySeparator(fullPath) || string.IsNullOrEmpty(Path.GetFileName(fullPath)))
            {
                throw Invalid($"Output '{outputPath}' must name a file.");
            }
            var root = Path.GetPathRoot(fullPath)!;
            foreach (var component in Components(fullPath[root.Length..]))
            {
                if (component.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    (OperatingSystem.IsWindows() && (component.EndsWith(' ') || component.EndsWith('.'))))
                {
                    throw Invalid($"Output '{outputPath}' contains an invalid or ambiguous path component.");
                }
            }
            ValidateLeaf(fullPath);
            return ResolveLinks(fullPath, new HashSet<string>(PathComparer));
        }
        catch (ArgumentException exception)
        {
            throw Invalid($"Invalid output path '{outputPath}': {exception.Message}", exception);
        }
        catch (NotSupportedException exception)
        {
            throw Invalid($"Invalid output path '{outputPath}': {exception.Message}", exception);
        }
        catch (IOException exception)
        {
            throw AccessFailure(outputPath, exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw AccessFailure(outputPath, exception);
        }
    }

    public static void RejectInputCollision(string outputPath, string projectPath, IEnumerable<string>? sourcePaths = null)
    {
        try
        {
            Check(Path.GetFullPath(projectPath));
            if (sourcePaths is not null)
            {
                foreach (var source in sourcePaths)
                {
                    if (Path.IsPathFullyQualified(source)) { Check(source); }
                }
            }
        }
        catch (ArgumentException exception)
        {
            throw new ToolException("TGH1002", $"Invalid project/source path: {exception.Message}", exception);
        }
        catch (IOException exception)
        {
            throw AccessFailure(outputPath, exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw AccessFailure(outputPath, exception);
        }

        void Check(string input)
        {
            if (PathComparer.Equals(outputPath, ResolveLinks(input, new HashSet<string>(PathComparer))))
            {
                throw Invalid($"Output '{outputPath}' would overwrite input '{input}'.");
            }
        }
    }

    public static void ValidateLeaf(string path)
    {
        var info = ExistingInfo(path);
        if (info is DirectoryInfo)
        {
            throw Invalid($"Output '{path}' is a directory.");
        }
        if (info is not null && (info.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw Invalid($"Output '{path}' is a link or reparse-point file; choose a regular output file.");
        }
    }

    private static string ResolveLinks(string fullPath, HashSet<string> activeLinks)
    {
        var root = Path.GetPathRoot(fullPath)!;
        var current = root;
        foreach (var component in Components(fullPath[root.Length..]))
        {
            current = Path.Combine(current, component);
            var info = ExistingInfo(current);
            if (info is not null && (info.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                if (!activeLinks.Add(current)) { throw new IOException($"Linked path cycle at '{current}'."); }
                var link = current;
                try
                {
                    var target = info.ResolveLinkTarget(returnFinalTarget: true)
                        ?? throw new IOException($"Cannot resolve linked path '{current}'.");
                    current = ResolveLinks(target.FullName, activeLinks);
                }
                finally { activeLinks.Remove(link); }
            }
        }
        return Path.GetFullPath(current);
    }

    private static IEnumerable<string> Components(string path) =>
        path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

    private static FileSystemInfo? ExistingInfo(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.Directory) != 0 ? new DirectoryInfo(path) : new FileInfo(path);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    private static ToolException Invalid(string message, Exception? inner = null) =>
        new("TGH1011", message, inner, exitCode: 2);

    private static ToolException AccessFailure(string path, Exception exception) =>
        new("TGH1012", $"Cannot inspect output/input paths for '{path}': {exception.Message}", exception);
}
