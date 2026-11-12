// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Xml.Linq;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;
using Xunit.Abstractions;
using static Azure.Deployments.Extensibility.TypeGeneration.Tests.Support.TypePackageTestReader;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Integration;

[Trait("Category", "Integration")]
public class PackageEndToEndTests(ITestOutputHelper output)
{
    private static readonly string[] OriginalVersions = ["2026-08-01", "2026-09-01"];
    private const string SettingsMarker = "    [TypeProperty(\"Repository settings\")]";

    [Fact]
    public async Task OfficialSample_HasCompletePackageSemanticsWithoutBuildingTheConsumer()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var inputs = Directory.GetFiles(fixture.DirectoryPath).ToDictionary(path => path, File.ReadAllBytes);
        var target = Path.Combine(fixture.DirectoryPath, "types.tgz");
        AssertOnlyPackageOutput(fixture);
        var result = await GenerateAsync(fixture, target);
        Assert.Contains("resourceNames=1 concreteResources=2", result.Output);
        AssertContoso(ReadArchive(target), OriginalVersions);
        await AssertRepeatedGenerationAsync(fixture, target);
        foreach (var (path, bytes) in inputs) { Assert.Equal<byte>(bytes, File.ReadAllBytes(path)); }
        AssertOnlyPackageOutput(fixture);
    }

    [Fact]
    public async Task SeparateDirectoriesAndReversedCompileOrder_ProduceIdenticalArchives()
    {
        using var first = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        using var second = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var project = XDocument.Load(second.ProjectPath);
        project.Root!.Add(new XElement("PropertyGroup", new XElement("EnableDefaultCompileItems", "false")));
        project.Root.Add(new XElement("ItemGroup",
            Directory.GetFiles(second.DirectoryPath, "*.cs").OrderDescending(StringComparer.Ordinal)
                .Select(path => new XElement("Compile", new XAttribute("Include", Path.GetFileName(path))))));
        project.Save(second.ProjectPath);
        var firstTarget = Path.Combine(first.DirectoryPath, "types.tgz");
        var secondTarget = Path.Combine(second.DirectoryPath, "types.tgz");
        await GenerateAsync(first, firstTarget);
        await GenerateAsync(second, secondTarget);
        AssertContoso(ReadArchive(firstTarget), OriginalVersions);
        AssertContoso(ReadArchive(secondTarget), OriginalVersions);
        Assert.Equal<byte>(File.ReadAllBytes(firstTarget), File.ReadAllBytes(secondTarget));
        AssertOnlyPackageOutput(first);
        AssertOnlyPackageOutput(second);
    }

    [Fact]
    public async Task AddedProperty_AppearsInEveryVersionWithoutBreakingTheSharedGraph()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var target = Path.Combine(fixture.DirectoryPath, "types.tgz");
        await GenerateAsync(fixture, target);
        var original = File.ReadAllBytes(target);
        AssertContoso(ReadArchive(target), OriginalVersions);
        ReplaceSource(fixture, SettingsMarker,
            "    [TypeProperty(\"Archived\")]\n    public bool Archived { get; set; }\n\n" + SettingsMarker);
        var result = await GenerateAsync(fixture, target);
        Assert.Contains("publication=written", result.Output);
        Assert.False(original.AsSpan().SequenceEqual(File.ReadAllBytes(target)));
        var package = ReadArchive(target);
        var body = AssertContoso(package, OriginalVersions, "archived");
        var archived = Property(body, "archived", 0, "Archived", package.Types);
        AssertScalar(archived, "BooleanType");
        await AssertRepeatedGenerationAsync(fixture, target);
        AssertOnlyPackageOutput(fixture);
    }

    [Theory]
    [InlineData("replace")]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("unversioned")]
    public async Task VersionChanges_OnlyChangeRegistrationsAndKeepSemanticNodes(string change)
    {
        string[] versions = change switch
        {
            "replace" => ["preview", "stable"],
            "add" => ["2026-08-01", "2026-09-01", "2026-10-01"],
            "remove" => ["2026-08-01"],
            "unversioned" => [],
            _ => throw new ArgumentOutOfRangeException(nameof(change)),
        };
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var target = Path.Combine(fixture.DirectoryPath, "types.tgz");
        await GenerateAsync(fixture, target);
        var original = ReadArchive(target);
        AssertContoso(original, OriginalVersions);
        ReplaceSource(fixture, "new[] { \"2026-08-01\", \"2026-09-01\" }",
            "new string[] { " + string.Join(", ", versions.Select(version => $"\"{version}\"")) + " }");
        var result = await GenerateAsync(fixture, target);
        Assert.Contains($"resourceNames=1 concreteResources={Math.Max(1, versions.Length)}", result.Output);
        Assert.Contains("publication=written", result.Output);
        var changed = ReadArchive(target);
        AssertContoso(changed, versions);
        Assert.Equal<string>(
            original.Types.EnumerateArray().Where(type => Kind(type) != "ResourceType").Select(type => type.GetRawText()),
            changed.Types.EnumerateArray().Where(type => Kind(type) != "ResourceType").Select(type => type.GetRawText()));
        Assert.Equal(original.Index.GetProperty("settings").GetRawText(), changed.Index.GetProperty("settings").GetRawText());
        await AssertRepeatedGenerationAsync(fixture, target);
        AssertOnlyPackageOutput(fixture);
    }

    [Theory]
    [InlineData("compiler", "TG1101")]
    [InlineData("unsupported", "TG1501")]
    [InlineData("duplicate-configuration", "TG1205")]
    public async Task InvalidInput_PreservesARealPublishedPackageAndCannotCreateANewOne(string scenario, string code)
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var target = Path.Combine(fixture.DirectoryPath, "types.tgz");
        await GenerateAsync(fixture, target);
        AssertContoso(ReadArchive(target), OriginalVersions);
        var bytes = File.ReadAllBytes(target);
        File.SetLastWriteTimeUtc(target, new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        var timestamp = File.GetLastWriteTimeUtc(target);
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Failure.cs"), scenario switch
        {
            "compiler" => "public class Failure { public MissingType Value { get; set; } }",
            "unsupported" => """
                using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
                [ResourceType("Invalid")] public class Failure { public double Value { get; set; } }
                """,
            "duplicate-configuration" => """
                using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
                [BicepConfiguration] public class Failure { public string? Name { get; set; } }
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        });
        var newTarget = Path.Combine(fixture.DirectoryPath, "unpublished", "types.tgz");
        foreach (var destination in new[] { target, newTarget })
        {
            output.WriteLine($"Checking {scenario} failure with output {destination}.");
            var result = await fixture.RunToolAsync("generate", "--project", fixture.ProjectPath, "--output", destination);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal("", result.Output);
            Assert.Contains($"error {code}", result.Error);
            Assert.Matches(@"Failure\.cs\([1-9][0-9]*,[1-9][0-9]*\)", result.Error);
            Assert.Equal<byte>(bytes, File.ReadAllBytes(target));
            Assert.Equal(timestamp, File.GetLastWriteTimeUtc(target));
            Assert.False(Directory.Exists(Path.GetDirectoryName(newTarget)));
            AssertOnlyPackageOutput(fixture);
        }
    }

    [Fact]
    public async Task ReferencedRoots_AreNotDiscoveredButBaseAndPropertyTypesAreBound()
    {
        using var dependency = await RealProjectFixture.CreateAsync();
        foreach (var (path, _) in ContosoAuthoringFixture.Sources)
        {
            File.Delete(Path.Combine(dependency.DirectoryPath, path));
        }
        File.WriteAllText(Path.Combine(dependency.DirectoryPath, "Contracts.cs"), """
            using Azure.Bicep.Types.Concrete;
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            namespace ReferencedModels;
            public class Identifiers
            {
                [TypeProperty("Repository name", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
                public required string Name { get; set; }
            }
            public class Details { public string? Label { get; set; } }
            [ResourceType("DependencyOnly", ApiVersions = new[] { "v1" })]
            public class DependencyOnly { public bool Enabled { get; set; } }
            [BicepConfiguration] public class DependencyConfiguration { public string? Token { get; set; } }
            """);
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var project = XDocument.Load(fixture.ProjectPath);
        project.Root!.Add(new XElement("ItemGroup",
            new XElement("ProjectReference", new XAttribute("Include", dependency.ProjectPath))));
        project.Save(fixture.ProjectPath);
        ReplaceSource(fixture, ": RepositoryIdentifiers", ": ReferencedModels.Identifiers");
        ReplaceSource(fixture, SettingsMarker,
            "    [TypeProperty(\"Referenced details\")]\n    public ReferencedModels.Details? Details { get; set; }\n\n" + SettingsMarker);
        await fixture.RestoreAsync();
        var target = Path.Combine(fixture.DirectoryPath, "types.tgz");
        await GenerateAsync(fixture, target);
        var package = ReadArchive(target);
        var body = AssertContoso(package, OriginalVersions, "details");
        var details = Nullable(Property(body, "details", 0, "Referenced details", package.Types), package.Types, "ObjectType");
        AssertObject(details, "Details", ["label"]);
        var label = Property(details, "label", 0, null, package.Types);
        AssertScalar(Nullable(label, package.Types, "StringType"), "StringType");
        AssertOnlyPackageOutput(dependency);
        AssertOnlyPackageOutput(fixture);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task SelectedConfigurationAndFramework_ReachTheActualPackage(string configuration)
    {
        using var fixture = await RealProjectFixture.CreateAsync("""
            <BicepExtensionName>$(Configuration)Name</BicepExtensionName>
            <BicepExtensionVersion>$(Configuration)-version</BicepExtensionVersion>
            <BicepExtensionIsSingleton Condition="'$(Configuration)' == 'Release'">true</BicepExtensionIsSingleton>
            <DefineConstants Condition="'$(Configuration)' == 'Release'">$(DefineConstants);SELECTED_RELEASE</DefineConstants>
            """, multiTarget: true);
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Selected.cs"), """
            #if WINDOWS && SELECTED_RELEASE
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            [ResourceType("Selected")] public class Selected { public bool Enabled { get; set; } }
            #endif
            """);
        var target = Path.Combine(fixture.DirectoryPath, "types.tgz");
        await GenerateAsync(fixture, target, "--configuration", configuration, "--framework", "net10.0-windows");
        var package = ReadArchive(target);
        var settings = package.Index.GetProperty("settings");
        Assert.Equal(configuration + "Name", settings.GetProperty("name").GetString());
        Assert.Equal(configuration + "-version", settings.GetProperty("version").GetString());
        Assert.Equal(configuration == "Release", settings.GetProperty("isSingleton").GetBoolean());
        var expected = configuration == "Release"
            ? new[] { "Repository@2026-08-01", "Repository@2026-09-01", "Selected" }
            : ["Repository@2026-08-01", "Repository@2026-09-01"];
        Assert.Equal<string>(expected, package.Index.GetProperty("resources").EnumerateObject().Select(property => property.Name));
        if (configuration == "Release")
        {
            var resource = Resolve(package.Index.GetProperty("resources").GetProperty("Selected"), package.Types, true);
            var body = Resolve(resource.GetProperty("body"), package.Types);
            AssertObject(body, "Selected", ["enabled"]);
            AssertScalar(Property(body, "enabled", 0, null, package.Types), "BooleanType");
        }
        AssertOnlyPackageOutput(fixture);
    }

    private async Task<ProcessResult> GenerateAsync(RealProjectFixture fixture, string target, params string[] options)
    {
        output.WriteLine($"Generating {fixture.ProjectPath} into {target}.");
        var result = await fixture.RunToolAsync(["generate", "--project", fixture.ProjectPath, "--output", target, .. options]);
        Assert.True(result.ExitCode == 0, result.Error);
        Assert.Equal("", result.Error);
        Assert.StartsWith("Type generation succeeded:", result.Output, StringComparison.Ordinal);
        return result;
    }

    private async Task AssertRepeatedGenerationAsync(RealProjectFixture fixture, string target)
    {
        var bytes = File.ReadAllBytes(target);
        File.SetLastWriteTimeUtc(target, new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        var timestamp = File.GetLastWriteTimeUtc(target);
        var result = await GenerateAsync(fixture, target);
        Assert.Contains("publication=unchanged", result.Output);
        Assert.Equal<byte>(bytes, File.ReadAllBytes(target));
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(target));
    }

    private static JsonElement AssertContoso((JsonElement Index, JsonElement Types) package, string[] versions, params string[] extraProperties)
    {
        var (index, types) = package;
        var settings = index.GetProperty("settings");
        Assert.Equal("Contoso", settings.GetProperty("name").GetString());
        Assert.Equal("1.2", settings.GetProperty("version").GetString());
        Assert.False(settings.GetProperty("isSingleton").GetBoolean());
        var keys = (versions.Length == 0 ? ["Repository"] : versions.Select(version => "Repository@" + version).ToArray())
            .Order(StringComparer.Ordinal).ToArray();
        var resources = index.GetProperty("resources").EnumerateObject().ToArray();
        Assert.Equal<string>(keys, resources.Select(resource => resource.Name));
        Assert.Equal<string>(keys, types.EnumerateArray().Where(type => Kind(type) == "ResourceType").Select(type => type.GetProperty("name").GetString()!));
        var concrete = resources.Select(resource => Resolve(resource.Value, types, true)).ToArray();
        Assert.Equal(keys.Length, concrete.Select(resource => resource.GetProperty("name").GetString()).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(concrete.Select(resource => resource.GetProperty("body").GetProperty("$ref").GetString()).Distinct(StringComparer.Ordinal));
        Assert.All(concrete, resource =>
        {
            Assert.Equal(31, resource.GetProperty("readableScopes").GetInt32());
            Assert.Equal(31, resource.GetProperty("writableScopes").GetInt32());
        });
        var body = Resolve(concrete[0].GetProperty("body"), types);
        AssertObject(body, "Repository", ["description", "name", "settings", "url", .. extraProperties]);
        AssertScalar(Property(body, "name", 17, "Repository name", types), "StringType");
        AssertScalar(Nullable(Property(body, "description", 0, "Description", types), types, "StringType"), "StringType");
        AssertScalar(Nullable(Property(body, "url", 2, "URL", types), types, "StringType"), "StringType");
        var configuration = Resolve(settings.GetProperty("configurationType"), types, true);
        AssertObject(configuration, "ContosoConfiguration", ["organization"]);
        AssertScalar(Nullable(Property(configuration, "organization", 0, "Organization", types), types, "StringType"), "StringType");

        var settingsUnion = Property(body, "settings", 0, "Repository settings", types);
        var settingsObject = Nullable(settingsUnion, types, "ObjectType");
        AssertObject(settingsObject, "RepositorySettings", ["labels", "parent", "topics", "visibility"]);
        var topics = Property(settingsObject, "topics", 0, "Repository topics", types);
        Assert.Equal("ArrayType", Kind(topics));
        AssertScalar(Resolve(topics.GetProperty("itemType"), types), "StringType");
        var labels = Property(settingsObject, "labels", 0, "Custom labels", types);
        Assert.Equal("ObjectType", Kind(labels));
        Assert.Equal("Map:global::Contoso.Models.Repository.settings.labels", labels.GetProperty("name").GetString());
        Assert.Empty(labels.GetProperty("properties").EnumerateObject());
        AssertScalar(Nullable(Resolve(labels.GetProperty("additionalProperties"), types), types, "StringType"), "StringType");
        var visibility = Property(settingsObject, "visibility", 0, "Visibility", types);
        Assert.Equal("UnionType", Kind(visibility));
        var literals = visibility.GetProperty("elements").EnumerateArray().Select(reference => Resolve(reference, types)).ToArray();
        Assert.All(literals, literal => Assert.Equal("StringLiteralType", Kind(literal)));
        Assert.Equal<string?>(["Private", "Public"], literals.Select(literal => literal.GetProperty("value").GetString()));
        var parent = Property(settingsObject, "parent", 0, "Parent settings", types);
        Assert.Equal(settingsUnion.GetRawText(), parent.GetRawText());
        Assert.Equal(
            body.GetProperty("properties").GetProperty("settings").GetProperty("type").GetProperty("$ref").GetString(),
            settingsObject.GetProperty("properties").GetProperty("parent").GetProperty("type").GetProperty("$ref").GetString());
        return body;
    }

    private static JsonElement Property(JsonElement owner, string name, int flags, string? description, JsonElement types)
    {
        var property = owner.GetProperty("properties").GetProperty(name);
        Assert.Equal(flags, property.GetProperty("flags").GetInt32());
        Assert.Equal(description is not null, property.TryGetProperty("description", out var actualDescription));
        if (description is not null) { Assert.Equal(description, actualDescription.GetString()); }
        return Resolve(property.GetProperty("type"), types);
    }

    private static JsonElement Nullable(JsonElement union, JsonElement types, string kind)
    {
        Assert.Equal("UnionType", Kind(union));
        var elements = union.GetProperty("elements");
        Assert.Equal(2, elements.GetArrayLength());
        AssertScalar(Resolve(elements[1], types), "NullType");
        var value = Resolve(elements[0], types);
        Assert.Equal(kind, Kind(value));
        return value;
    }

    private static void AssertObject(JsonElement value, string name, string[] properties)
    {
        Assert.Equal("ObjectType", Kind(value));
        Assert.Equal(name, value.GetProperty("name").GetString());
        Assert.False(value.TryGetProperty("additionalProperties", out _));
        Assert.False(value.TryGetProperty("sensitive", out _));
        Assert.Equal<string>(properties.Order(StringComparer.Ordinal), value.GetProperty("properties").EnumerateObject().Select(property => property.Name));
    }

    private static void AssertScalar(JsonElement value, string kind)
    {
        Assert.Equal(kind, Kind(value));
        Assert.Equal("$type", Assert.Single(value.EnumerateObject()).Name);
    }

    private static string Kind(JsonElement value) => value.GetProperty("$type").GetString()!;

    private static void ReplaceSource(RealProjectFixture fixture, string oldValue, string newValue)
    {
        var path = Path.Combine(fixture.DirectoryPath, "Repository.cs");
        var source = File.ReadAllText(path);
        Assert.Equal(1, source.Split(oldValue, StringSplitOptions.None).Length - 1);
        File.WriteAllText(path, source.Replace(oldValue, newValue, StringComparison.Ordinal));
    }

    private static void AssertOnlyPackageOutput(RealProjectFixture fixture)
    {
        var paths = Directory.GetFiles(fixture.DirectoryPath, "*", SearchOption.AllDirectories);
        Assert.DoesNotContain(paths, path => Path.GetExtension(path) is ".dll" or ".pdb" or ".tmp" ||
            Path.GetFileName(path) is "index.json" or "types.json");
    }
}
