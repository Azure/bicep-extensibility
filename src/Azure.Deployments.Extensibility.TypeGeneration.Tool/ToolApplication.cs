// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

            var options = ToolOptions.Parse(args);
            var analysis = await ProjectAnalyzer.AnalyzeAsync(options, cancellationToken);
            foreach (var message in analysis.DiagnosticMessages)
            {
                await error.WriteLineAsync(message);
            }

            if (!analysis.Result.Succeeded)
            {
                return 1;
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
