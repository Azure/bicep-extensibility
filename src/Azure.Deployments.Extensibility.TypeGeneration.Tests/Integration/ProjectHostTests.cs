// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Integration;

[Trait("Category", "Integration")]
public class ProjectHostTests
{
    [Fact]
    public async Task RealProject_MatchesGoldenWithoutBuildingOrExecutingTheProject()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "NeverExecute.cs"), """
            using System.Runtime.CompilerServices;
            internal static class NeverExecute
            {
                [ModuleInitializer] internal static void Initialize() => throw new System.Exception("Do not execute the project.");
            }
            """);
        var filesBefore = Directory.GetFiles(fixture.DirectoryPath, "*.json", SearchOption.AllDirectories);
        var analysis = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.True(analysis.Result.Succeeded, string.Join(Environment.NewLine, analysis.DiagnosticMessages));
        Assert.True(SemanticModelComparer.AreEqual(ContosoAuthoringFixture.CreateExpected(), analysis.Result.Model));
        Assert.DoesNotContain(analysis.Result.Diagnostics, diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.dll", SearchOption.AllDirectories));
        Assert.Equal(filesBefore, Directory.GetFiles(fixture.DirectoryPath, "*.json", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ImportedAndConditionalMetadata_UsesSelectedFrameworkAndExplicitOverrides()
    {
        using var fixture = await RealProjectFixture.CreateAsync("""
            <BicepExtensionName Condition="'$(Configuration)' == 'Debug'">Imported</BicepExtensionName>
            <BicepExtensionVersion>project-version</BicepExtensionVersion>
            <BicepExtensionIsSingleton>true</BicepExtensionIsSingleton>
            <BicepExtensionName Condition="'$(TargetFramework)' == 'net10.0-windows'">Windows</BicepExtensionName>
            """, multiTarget: true);
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "WindowsOnly.cs"), """
            #if WINDOWS
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            [ResourceType("WindowsOnly")] public class WindowsOnly { public bool Enabled { get; set; } }
            #endif
            """);
        var imported = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, "net10.0-windows"));
        Assert.True(imported.Result.Succeeded, string.Join(Environment.NewLine, imported.DiagnosticMessages));
        Assert.Equal("Windows", imported.Result.Model!.Name);
        Assert.Equal("project-version", imported.Result.Model.Version);
        Assert.True(imported.Result.Model.IsSingleton);
        Assert.Contains(imported.Result.Model.ResourceTypes, resource => resource.Name == "WindowsOnly");

        var overridden = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, "net10.0", "Override", "override-version", false));
        Assert.True(overridden.Result.Succeeded, string.Join(Environment.NewLine, overridden.DiagnosticMessages));
        Assert.Equal("Override", overridden.Result.Model!.Name);
        Assert.Equal("override-version", overridden.Result.Model.Version);
        Assert.False(overridden.Result.Model.IsSingleton);
        Assert.DoesNotContain(overridden.Result.Model.ResourceTypes, resource => resource.Name == "WindowsOnly");
    }

    [Theory]
    [InlineData(null, "TG1001", "TG1002")]
    [InlineData("<BicepExtensionName>Name</BicepExtensionName>", "TG1002", null)]
    [InlineData("<BicepExtensionVersion>1</BicepExtensionVersion>", "TG1001", null)]
    public async Task MissingMetadata_DoesNotReturnAModel(string? properties, string first, string? second)
    {
        using var fixture = await RealProjectFixture.CreateAsync(properties ?? "");
        var analysis = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.False(analysis.Result.Succeeded);
        Assert.Null(analysis.Result.Model);
        Assert.Contains(analysis.Result.Diagnostics, diagnostic => diagnostic.Code == first);
        if (second is not null)
        {
            Assert.Contains(analysis.Result.Diagnostics, diagnostic => diagnostic.Code == second);
        }
    }

    [Fact]
    public async Task InvalidSingleton_IsNotFalse_AndAnOverrideCanReplaceIt()
    {
        using var fixture = await RealProjectFixture.CreateAsync("<BicepExtensionIsSingleton>yes</BicepExtensionIsSingleton>");
        var exception = await Assert.ThrowsAsync<ToolException>(() => ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath)));
        Assert.Equal("TGH1006", exception.Code);
        var analysis = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, Name: "Override", Version: "1", Singleton: false));
        Assert.True(analysis.Result.Succeeded);
        Assert.False(analysis.Result.Model!.IsSingleton);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("net8.0")]
    public async Task MultiTarget_RequiresAnExplicitDeclaredFramework(string? framework)
    {
        using var fixture = await RealProjectFixture.CreateAsync(restore: false, multiTarget: true);
        var exception = await Assert.ThrowsAsync<ToolException>(() => ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, framework)));
        Assert.Equal("TGH1003", exception.Code);
        Assert.Contains(fixture.ProjectPath, exception.Message);
    }

    [Fact]
    public async Task MissingAssets_IsExplicitAndDoesNotRestoreAutomatically()
    {
        using var fixture = await RealProjectFixture.CreateAsync(restore: false);
        var exception = await Assert.ThrowsAsync<ToolException>(() => ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath)));
        Assert.Equal("TGH1005", exception.Code);
        Assert.Contains("dotnet restore", exception.Message);
        Assert.False(Directory.Exists(Path.Combine(fixture.DirectoryPath, "obj")));
    }

    [Fact]
    public async Task MultiTarget_SharedOutputPathsAreRejectedRatherThanPickingTheFirst()
    {
        using var fixture = await RealProjectFixture.CreateAsync(
            "<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>", multiTarget: true);
        var exception = await Assert.ThrowsAsync<ToolException>(() =>
            ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, "net10.0-windows", "Name", "1")));
        Assert.Equal("TGH1003", exception.Code);
        Assert.Contains("uniquely select", exception.Message);
    }

    [Fact]
    public async Task CancellationDuringRealProjectLoading_Returns130()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(130, await ToolApplication.RunAsync(["--project", fixture.ProjectPath], output, error, cancellation.Token));
        Assert.Equal("", output.ToString());
        Assert.Contains("TGH0002", error.ToString());
    }

    [Fact]
    public async Task MissingProject_ReturnsExitOneAndNoOutput()
    {
        using var fixture = await RealProjectFixture.CreateAsync(restore: false);
        var result = await RunAsync(Path.Combine(fixture.DirectoryPath, "missing.csproj"));
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.Contains("TGH1002", result.Error);
    }

    [Theory]
    [InlineData("<Project>")]
    [InlineData("<Project Sdk=\"Missing.Sdk.For.TypeGeneration.Tests\" />")]
    public async Task InvalidProjectOrUnavailableSdk_HasProjectContext(string source)
    {
        using var fixture = await RealProjectFixture.CreateAsync(restore: false);
        File.WriteAllText(fixture.ProjectPath, source);
        var result = await RunAsync(fixture.ProjectPath);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.Contains("TGH1002", result.Error);
        Assert.Contains(fixture.ProjectPath, result.Error);
    }

    [Fact]
    public async Task CompilationError_HasCodeFileAndHumanReadablePosition()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Broken.cs"), "public class Broken { public MissingType Value { get; set; } }");
        var result = await RunAsync(fixture.ProjectPath);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.Contains("TG1101", result.Error);
        Assert.Contains("CS0246", result.Error);
        Assert.Contains("Broken.cs(1,", result.Error);
    }

    [Fact]
    public async Task WorkspaceFailure_IsNotAnEmptySuccessfulCompilation()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Directory.Build.targets"), """
            <Project><Target Name="FailDesignTime" BeforeTargets="CoreCompile" Condition="'$(DesignTimeBuild)' == 'true'">
              <Error Text="Intentional workspace failure" />
            </Target></Project>
            """);
        var result = await RunAsync(fixture.ProjectPath);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.Contains("TGH1004", result.Error);
        Assert.Contains("Intentional workspace failure", result.Error);
    }

    [Fact]
    public async Task CompilerWarnings_AreReportedWithoutHidingSuccessfulOutput()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Warning.cs"), "#warning Intentional compiler warning");
        var result = await RunAsync(fixture.ProjectPath);
        Assert.True(result.ExitCode == 0, result.Error);
        Assert.Contains("extension \"Contoso\"", result.Output);
        Assert.Contains("warning TG1101", result.Error);
        Assert.Contains("Warning.cs(1,", result.Error);
    }

    [Fact]
    public async Task WorkspaceFailure_IsNotDemotedBasedOnTheOriginalMsbuildEventText()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Directory.Build.targets"), """
            <Project><Target Name="WarnDesignTime" BeforeTargets="CoreCompile" Condition="'$(DesignTimeBuild)' == 'true'">
              <Warning Text="Intentional workspace warning" />
            </Target></Project>
            """);
        var result = await RunAsync(fixture.ProjectPath);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.Contains("error TGH1004", result.Error);
        Assert.Contains("Intentional workspace warning", result.Error);
    }

    [Fact]
    public async Task MissingAssemblyReference_IsAnErrorRatherThanAnyType()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        var project = System.Xml.Linq.XDocument.Load(fixture.ProjectPath);
        project.Root!.Add(new System.Xml.Linq.XElement("ItemGroup",
            new System.Xml.Linq.XElement("Reference", new System.Xml.Linq.XAttribute("Include", "MissingAssembly"),
                new System.Xml.Linq.XElement("HintPath", "MissingAssembly.dll"))));
        project.Save(fixture.ProjectPath);
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Missing.cs"), """
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            [ResourceType("Missing")] public class Missing { public MissingAssembly.Schema? Value { get; set; } }
            """);
        var result = await RunAsync(fixture.ProjectPath);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.Contains("MissingAssembly", result.Error);
    }

    [Fact]
    public async Task MissingProjectReference_IsReported()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        var project = System.Xml.Linq.XDocument.Load(fixture.ProjectPath);
        project.Root!.Add(new System.Xml.Linq.XElement("ItemGroup",
            new System.Xml.Linq.XElement("ProjectReference", new System.Xml.Linq.XAttribute("Include", "Missing.csproj"))));
        project.Save(fixture.ProjectPath);
        var result = await RunAsync(fixture.ProjectPath);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.Contains("Missing.csproj", result.Error);
    }

    [Fact]
    public async Task FreshCliProcess_RegistersMSBuild_AndKeepsStdoutSeparate()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        var success = await fixture.RunToolAsync("--project", fixture.ProjectPath, "--format", "tree");
        Assert.True(success.ExitCode == 0, success.Output + success.Error);
        Assert.Contains("extension \"Contoso\"", success.Output);
        Assert.Contains("versions=[\"2026-08-01\", \"2026-09-01\"]", success.Output);
        Assert.DoesNotContain("error ", success.Output);
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.dll", SearchOption.AllDirectories));

        var invalid = await fixture.RunToolAsync("--project", fixture.ProjectPath, "--singleton", "1");
        Assert.Equal(2, invalid.ExitCode);
        Assert.Equal("", invalid.Output);
        Assert.Contains("TGH0001", invalid.Error);

        var missing = await fixture.RunToolAsync("--project", Path.Combine(fixture.DirectoryPath, "missing.csproj"));
        Assert.Equal(1, missing.ExitCode);
        Assert.Equal("", missing.Output);
        Assert.Contains("TGH1002", missing.Error);
    }

    [Fact]
    public async Task SemanticError_ReturnsNoModelOrSuccessOutput()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Duplicate.cs"), """
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            [BicepConfiguration] public class DuplicateConfiguration;
            """);
        var result = await RunAsync(fixture.ProjectPath);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.Contains("TG1205", result.Error);
        Assert.Contains(".cs(", result.Error);
    }

    private static async Task<ProcessResult> RunAsync(string projectPath)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await ToolApplication.RunAsync(["--project", projectPath], output, error);
        return new(exit, output.ToString(), error.ToString());
    }
}
