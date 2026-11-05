// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Azure.Deployments.Extensibility.TypeGeneration.Backend;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal static class ToolApplication
{
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (args is ["--help"] or ["-h"])
            {
                await output.WriteLineAsync(ToolOptions.Usage);
                return 0;
            }
            if (args is ["inspect", "--help" or "-h"])
            {
                await output.WriteLineAsync(ToolOptions.InspectUsage);
                return 0;
            }
            if (args is ["generate", "--help" or "-h"])
            {
                await output.WriteLineAsync(ToolOptions.GenerateUsage);
                return 0;
            }

            var options = ToolOptions.Parse(args);
            string? outputPath = null;
            if (options.Command == ToolCommand.Generate)
            {
                outputPath = TypeGenerationOutputPath.Resolve(options.OutputPath!);
                TypeGenerationOutputPath.RejectInputCollision(outputPath, options.ProjectPath);
            }
            var analysis = await ProjectAnalyzer.AnalyzeAsync(options, cancellationToken);
            foreach (var message in analysis.DiagnosticMessages)
            {
                await error.WriteLineAsync(message);
            }

            if (!analysis.Result.Succeeded)
            {
                return 1;
            }

            if (options.Command == ToolCommand.Generate)
            {
                TypeGenerationOutputPath.RejectInputCollision(outputPath!, options.ProjectPath, analysis.DiagnosticFormatter.SourcePaths);
                var model = analysis.Result.Model!;
                var generation = new TypeGenerationPackageGenerator().Generate(model, cancellationToken);
                foreach (var message in analysis.DiagnosticFormatter.Format(generation.Diagnostics, cancellationToken))
                {
                    await error.WriteLineAsync(message);
                }
                if (!generation.Succeeded) { return 1; }
                var publication = await new TypeGenerationPackagePublisher().PublishAsync(
                    outputPath!, generation.Package!.ArchiveBytes, cancellationToken);
                var summary = $"Type generation succeeded: extension={JsonSerializer.Serialize(model.Name)} " +
                    $"version={JsonSerializer.Serialize(model.Version)} " +
                    $"resourceNames={model.ResourceTypes.Select(resource => resource.Name).Distinct(StringComparer.Ordinal).Count()} " +
                    $"concreteResources={model.ResourceTypes.Sum(resource => Math.Max(1L, resource.ApiVersions.Length))} " +
                    $"output={JsonSerializer.Serialize(publication.OutputPath)} " +
                    $"publication={(publication.Changed ? "written" : "unchanged")} validation=internal-json-and-archive";
                try
                {
                    await output.WriteLineAsync(summary);
                }
                catch (IOException exception)
                {
                    throw new ToolException("TGH1014",
                        $"Package publication completed at '{publication.OutputPath}' " +
                        $"({(publication.Changed ? "written" : "unchanged")}), but the summary could not be written: {exception.Message}", exception);
                }
                return 0;
            }

            var tree = EtsmTreeWriter.Render(analysis.Result.Model!, cancellationToken);
            await output.WriteAsync(tree.AsMemory(), cancellationToken);
            return 0;
        }
        catch (ToolException exception)
        {
            await error.WriteLineAsync($"error {exception.Code}: {exception.Message}");
            if (exception.ExitCode == 2)
            {
                await error.WriteLineAsync(ToolOptions.Usage);
            }

            return exception.ExitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await error.WriteLineAsync("error TGH0002: Analysis cancelled.");
            return 130;
        }
        catch (IOException exception)
        {
            await error.WriteLineAsync($"error TGH1008: I/O failure: {exception.Message}");
            return 1;
        }
    }
}
