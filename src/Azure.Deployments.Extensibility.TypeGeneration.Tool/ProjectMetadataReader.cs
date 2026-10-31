// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Build.Evaluation;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal sealed record EvaluatedProject(
    ExtensionBuildMetadata Metadata,
    Dictionary<string, string> GlobalProperties,
    string Framework,
    string OutputPath);

internal static class ProjectMetadataReader
{
    public static EvaluatedProject Read(ToolOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var globals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Configuration"] = "Debug",
        };
        using var collection = new ProjectCollection();
        var project = collection.LoadProject(options.ProjectPath, globals, toolsVersion: null);
        var frameworks = project.GetPropertyValue("TargetFrameworks")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (frameworks.Length == 0 && project.GetPropertyValue("TargetFramework") is { Length: > 0 } framework)
        {
            frameworks = [framework];
        }

        if (frameworks.Length == 0)
        {
            throw new ToolException("TGH1003", $"Project '{options.ProjectPath}' does not declare a target framework.");
        }

        if (options.Framework is null && frameworks.Length > 1)
        {
            throw new ToolException("TGH1003",
                $"Project '{options.ProjectPath}' targets {string.Join(", ", frameworks)}. Select one with --framework.");
        }

        var selected = options.Framework ?? frameworks[0];
        if (!frameworks.Contains(selected, StringComparer.OrdinalIgnoreCase))
        {
            throw new ToolException("TGH1003",
                $"Framework '{selected}' is not declared by '{options.ProjectPath}'. Available: {string.Join(", ", frameworks)}.");
        }

        globals["TargetFramework"] = selected;
        collection.UnloadProject(project);
        project = collection.LoadProject(options.ProjectPath, globals, toolsVersion: null);
        cancellationToken.ThrowIfCancellationRequested();

        var singleton = options.Singleton ?? ReadSingleton(project.GetPropertyValue("BicepExtensionIsSingleton"), options.ProjectPath);
        var metadata = new ExtensionBuildMetadata(
            options.Name ?? project.GetPropertyValue("BicepExtensionName"),
            options.Version ?? project.GetPropertyValue("BicepExtensionVersion"),
            singleton);

        var assets = project.GetPropertyValue("ProjectAssetsFile");
        if (string.IsNullOrWhiteSpace(assets))
        {
            throw new ToolException("TGH1005", $"Project '{options.ProjectPath}' has no ProjectAssetsFile. Use a restored SDK-style C# project.");
        }

        assets = Path.GetFullPath(assets, Path.GetDirectoryName(options.ProjectPath)!);
        if (!File.Exists(assets))
        {
            throw new ToolException("TGH1005",
                $"Restore assets are missing for '{options.ProjectPath}': '{assets}'. Run dotnet restore on this project first.");
        }

        var outputPath = Path.GetFullPath(project.GetPropertyValue("TargetPath"), Path.GetDirectoryName(options.ProjectPath)!);
        // A workspace-global TFM would also override referenced projects' own frameworks.
        globals.Remove("TargetFramework");
        return new(metadata, globals, selected, outputPath);
    }

    private static bool ReadSingleton(string value, string projectPath)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (!bool.TryParse(value, out var singleton))
        {
            throw new ToolException("TGH1006",
                $"BicepExtensionIsSingleton in '{projectPath}' must be true or false, not '{value}'.");
        }

        return singleton;
    }
}
