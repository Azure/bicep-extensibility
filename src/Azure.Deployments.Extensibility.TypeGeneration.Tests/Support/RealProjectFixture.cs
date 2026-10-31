// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;

internal sealed class RealProjectFixture : IDisposable
{
    private RealProjectFixture(string projectFileName = "Fixture.csproj")
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), "TypeGenerationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
        ProjectPath = Path.Combine(DirectoryPath, projectFileName);
    }

    public string DirectoryPath { get; }

    public string ProjectPath { get; }

    public static string RepositoryRoot
    {
        get
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Azure.Deployments.Extensibility.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new InvalidOperationException("Cannot locate the repository root from the test output directory.");
        }
    }

    public static string ToolAssemblyPath => Path.Combine(RepositoryRoot, "src",
        "Azure.Deployments.Extensibility.TypeGeneration.Tool", "bin",
        typeof(RealProjectFixture).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration,
        "net10.0", "Azure.Deployments.Extensibility.TypeGeneration.Tool.dll");

    public static async Task<RealProjectFixture> CreateAsync(
        string? properties = null,
        bool restore = true,
        bool multiTarget = false)
    {
        var fixture = new RealProjectFixture();
        var ready = false;
        try
        {
            var sdk = Path.Combine(RepositoryRoot, "src", "Azure.Deployments.Extensibility.TypeGeneration.Sdk",
                "Azure.Deployments.Extensibility.TypeGeneration.Sdk.csproj");
            var reference = new XElement("ProjectReference", new XAttribute("Include", sdk)).ToString();
            File.WriteAllText(fixture.ProjectPath, $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    {{(multiTarget ? "<TargetFrameworks>net10.0;net10.0-windows</TargetFrameworks>" : "<TargetFramework>net10.0</TargetFramework>")}}
                    <Nullable>enable</Nullable>
                    <LangVersion>12.0</LangVersion>
                    <NuGetAudit>false</NuGetAudit>
                  </PropertyGroup>
                  <Import Project="Extension.props" />
                  <ItemGroup>{{reference}}</ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Extension.props"), $$"""
                <Project><PropertyGroup>
                {{properties ?? "<BicepExtensionName>Contoso</BicepExtensionName><BicepExtensionVersion>1.2</BicepExtensionVersion>"}}
                </PropertyGroup></Project>
                """);
            fixture.IsolateBuildSettings();
            foreach (var (path, source) in ContosoAuthoringFixture.Sources)
            {
                File.WriteAllText(Path.Combine(fixture.DirectoryPath, path), source);
            }

            if (restore)
            {
                await fixture.RestoreAsync();
            }

            ready = true;
            return fixture;
        }
        finally
        {
            if (!ready)
            {
                fixture.Dispose();
            }
        }
    }

    public static async Task<RealProjectFixture> CopyProjectAsync(string projectPath)
    {
        var fixture = new RealProjectFixture(Path.GetFileName(projectPath));
        var ready = false;
        try
        {
            var sourceDirectory = Path.GetDirectoryName(projectPath)!;
            var project = XDocument.Load(projectPath);
            foreach (var reference in project.Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")
                    ?? throw new InvalidOperationException($"Project reference in '{projectPath}' has no Include.");
                include.Value = Path.GetFullPath(include.Value.Replace('\\', Path.DirectorySeparatorChar), sourceDirectory);
            }

            project.Save(fixture.ProjectPath);
            foreach (var source in Directory.GetFiles(sourceDirectory, "*.cs"))
            {
                File.Copy(source, Path.Combine(fixture.DirectoryPath, Path.GetFileName(source)));
            }

            fixture.IsolateBuildSettings();
            await fixture.RestoreAsync();
            ready = true;
            return fixture;
        }
        finally
        {
            if (!ready)
            {
                fixture.Dispose();
            }
        }
    }

    private void IsolateBuildSettings()
    {
        File.WriteAllText(Path.Combine(DirectoryPath, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(DirectoryPath, "Directory.Build.targets"), "<Project />");
        File.Copy(Path.Combine(RepositoryRoot, "global.json"), Path.Combine(DirectoryPath, "global.json"));
    }

    public async Task RestoreAsync()
    {
        var result = await RunProcessAsync(DirectoryPath,
            ["restore", ProjectPath, "--no-dependencies", "--nologo", "-v", "minimal"]);
        Assert.True(result.ExitCode == 0, result.Output + result.Error);
    }

    public Task<ProcessResult> RunToolAsync(params string[] args) =>
        RunProcessAsync(DirectoryPath, [ToolAssemblyPath, .. args]);

    public static async Task<ProcessResult> RunProcessAsync(string workingDirectory, string[] args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return new(process.ExitCode, await output, await error);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
}

internal sealed record ProcessResult(int ExitCode, string Output, string Error);
