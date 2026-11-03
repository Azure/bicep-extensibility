// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal enum ToolCommand
{
    Inspect,
    Generate,
}

internal sealed record ToolOptions(
    string ProjectPath,
    string? Framework = null,
    string? Name = null,
    string? Version = null,
    bool? Singleton = null,
    string Configuration = "Debug",
    ToolCommand Command = ToolCommand.Inspect,
    string? OutputPath = null)
{
    public const string InspectUsage =
        "Usage: TypeGeneration.Tool [inspect] --project <project.csproj> [--format tree] " +
        "[--framework <TFM>] [--configuration <configuration>] [--name <name>] [--version <version>] [--singleton true|false]";

    public const string GenerateUsage =
        "Usage: TypeGeneration.Tool generate --project <project.csproj> --output <types.tgz> " +
        "[--framework <TFM>] [--configuration <configuration>] [--name <name>] [--version <version>] [--singleton true|false]";

    public const string Usage = InspectUsage + "\n" + GenerateUsage;

    public static ToolOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var command = ToolCommand.Inspect;
        var start = 0;
        if (args.Length > 0 && args[0] is "inspect" or "generate")
        {
            command = args[0] == "generate" ? ToolCommand.Generate : ToolCommand.Inspect;
            start = 1;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = start; index < args.Length; index += 2)
        {
            var option = args[index];
            if (option is not ("--project" or "--format" or "--framework" or "--name" or "--version" or "--singleton" or "--configuration" or "--output"))
            {
                throw Invalid($"Unknown option '{option}'.");
            }
            if (command == ToolCommand.Generate && option == "--format")
            {
                throw Invalid("Option '--format' is only supported by inspect.");
            }
            if (command == ToolCommand.Inspect && option == "--output")
            {
                throw Invalid("Option '--output' is only supported by generate.");
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
        if (command == ToolCommand.Generate && !values.ContainsKey("--output"))
        {
            throw Invalid("Option '--output' is required for generate.");
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
            values.GetValueOrDefault("--name"), values.GetValueOrDefault("--version"), singleton,
            values.GetValueOrDefault("--configuration") ?? "Debug", command, values.GetValueOrDefault("--output"));
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
