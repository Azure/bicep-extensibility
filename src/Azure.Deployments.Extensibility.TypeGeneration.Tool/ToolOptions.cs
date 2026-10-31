// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal sealed record ToolOptions(
    string ProjectPath,
    string? Framework = null,
    string? Name = null,
    string? Version = null,
    bool? Singleton = null)
{
    public const string Usage =
        "Usage: TypeGeneration.Tool --project <project.csproj> [--format tree] " +
        "[--framework <TFM>] [--name <name>] [--version <version>] [--singleton true|false]";

    public static ToolOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            var option = args[index];
            if (option is not ("--project" or "--format" or "--framework" or "--name" or "--version" or "--singleton"))
            {
                throw Invalid($"Unknown option '{option}'.");
            }

            if (index + 1 == args.Length || string.IsNullOrWhiteSpace(args[index + 1]) ||
                args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw Invalid($"Option '{option}' requires a value.");
            }

            if (!values.TryAdd(option, args[index + 1]))
            {
                throw Invalid($"Option '{option}' was specified more than once.");
            }
        }

        if (!values.TryGetValue("--project", out var project))
        {
            throw Invalid("Option '--project' is required.");
        }

        if (values.TryGetValue("--format", out var format) && format != "tree")
        {
            throw Invalid("Only '--format tree' is supported.");
        }

        bool? singleton = null;
        if (values.TryGetValue("--singleton", out var value))
        {
            if (!bool.TryParse(value, out var parsed))
            {
                throw Invalid("Option '--singleton' must be true or false.");
            }

            singleton = parsed;
        }

        return new(project, values.GetValueOrDefault("--framework"),
            values.GetValueOrDefault("--name"), values.GetValueOrDefault("--version"), singleton);
    }

    private static ToolException Invalid(string message) => new("TGH0001", message, exitCode: 2);
}

internal sealed class ToolException(
    string code,
    string message,
    Exception? innerException = null,
    int exitCode = 1) : Exception(message, innerException)
{
    public string Code { get; } = code;

    public int ExitCode { get; } = exitCode;
}
