// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Lowering;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Backend;

[Trait("Category", "Unit")]
public class TypeGenerationJsonWriterTests
{
    [Fact]
    public void MinimalPackage_MatchesHandwrittenGoldenAndIndependentSemanticAssertions()
    {
        var result = Write(Lower(Minimal()));
        AssertGolden("MinimalPackage.index.json", result.IndexJson);
        AssertGolden("MinimalPackage.types.json", result.TypesJson);
        using var index = JsonDocument.Parse(result.IndexJson.ToArray());
        using var types = JsonDocument.Parse(result.TypesJson.ToArray());
        var root = index.RootElement;
        var graph = types.RootElement;
        var first = Resolve(root.GetProperty("resources").GetProperty("Repository@v1"), graph, true);
        var second = Resolve(root.GetProperty("resources").GetProperty("Repository@v2"), graph, true);
        Assert.NotEqual(first.GetProperty("name").GetString(), second.GetProperty("name").GetString());
        Assert.Equal(first.GetProperty("body").GetProperty("$ref").GetString(), second.GetProperty("body").GetProperty("$ref").GetString());
        var body = Resolve(first.GetProperty("body"), graph, false);
        Assert.Equal(17, body.GetProperty("properties").GetProperty("name").GetProperty("flags").GetInt32());
        var description = Resolve(body.GetProperty("properties").GetProperty("description").GetProperty("type"), graph, false);
        Assert.Equal("UnionType", description.GetProperty("$type").GetString());
        Assert.Equal("NullType", Resolve(description.GetProperty("elements")[1], graph, false).GetProperty("$type").GetString());
        var config = Resolve(root.GetProperty("settings").GetProperty("configurationType"), graph, true);
        Assert.Equal("ContosoConfiguration", config.GetProperty("name").GetString());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void EmptyResourcesAndOptionalConfiguration_PreserveRequiredSettings(bool configuration, bool singleton)
    {
        var result = Write(Lower(Model() with { IsSingleton = singleton, Configuration = configuration ? Object("Config") : null }));
        using var index = JsonDocument.Parse(result.IndexJson.ToArray());
        using var types = JsonDocument.Parse(result.TypesJson.ToArray());
        Assert.Empty(index.RootElement.GetProperty("resources").EnumerateObject());
        var settings = index.RootElement.GetProperty("settings");
        Assert.Equal(singleton, settings.GetProperty("isSingleton").GetBoolean());
        Assert.Equal(configuration, settings.TryGetProperty("configurationType", out _));
        Assert.Equal(configuration ? 1 : 0, types.RootElement.GetArrayLength());
        Assert.Equal("index.json", TypeGenerationSerializedPackage.IndexFileName);
        Assert.Equal("types.json", TypeGenerationSerializedPackage.TypesFileName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MandatoryZerosAndOptionalValues_AreNotConfused(bool sensitive)
    {
        var text = new StringTypeDefinition { Sensitive = sensitive, MinLength = 0, MaxLength = 0, Pattern = "" };
        var body = Object("Body", Property("empty", text) with { Description = "" }, Property("absent", new BooleanTypeDefinition())) with { Sensitive = sensitive };
        var resource = Resource("R", [], body) with { ReadableScopes = ResourceScope.None, WritableScopes = ResourceScope.None };
        var result = Write(Lower(Model(resource)));
        using var index = JsonDocument.Parse(result.IndexJson.ToArray());
        using var types = JsonDocument.Parse(result.TypesJson.ToArray());
        var node = Resolve(index.RootElement.GetProperty("resources").GetProperty("R"), types.RootElement, true);
        Assert.Equal(0, node.GetProperty("readableScopes").GetInt32());
        Assert.Equal(0, node.GetProperty("writableScopes").GetInt32());
        var actualBody = Resolve(node.GetProperty("body"), types.RootElement, false);
        Assert.Equal(sensitive, actualBody.TryGetProperty("sensitive", out _));
        var empty = actualBody.GetProperty("properties").GetProperty("empty");
        Assert.Equal(0, empty.GetProperty("flags").GetInt32());
        Assert.Equal("", empty.GetProperty("description").GetString());
        Assert.False(actualBody.GetProperty("properties").GetProperty("absent").TryGetProperty("description", out _));
        var actualText = Resolve(empty.GetProperty("type"), types.RootElement, false);
        Assert.Equal(sensitive, actualText.TryGetProperty("sensitive", out _));
        Assert.Equal(0, actualText.GetProperty("minLength").GetInt64());
        Assert.Equal(0, actualText.GetProperty("maxLength").GetInt64());
        Assert.Equal("", actualText.GetProperty("pattern").GetString());
    }

    [Fact]
    public void ScalarBoundsNestedCollectionsAndEnumOrder_SurviveSerialization()
    {
        var text = new StringTypeDefinition();
        var nullable = Union(text, new NullTypeDefinition());
        var source = Object("Body",
            Property("integer", new IntegerTypeDefinition { MinValue = long.MinValue, MaxValue = long.MaxValue }),
            Property("any", new AnyTypeDefinition()),
            Property("array", Union(new ArrayTypeDefinition { ItemType = nullable, MinLength = 0, MaxLength = 7 }, new NullTypeDefinition())),
            Property("map", Object("Map") with { AdditionalProperties = nullable }),
            Property("enum", Union(new StringLiteralTypeDefinition { Value = "Zulu" }, new StringLiteralTypeDefinition { Value = "Alpha" })));
        var result = Write(Lower(Model(Resource("R", [], source))));
        using var index = JsonDocument.Parse(result.IndexJson.ToArray());
        using var types = JsonDocument.Parse(result.TypesJson.ToArray());
        var body = Resolve(Resolve(index.RootElement.GetProperty("resources").GetProperty("R"), types.RootElement, true).GetProperty("body"), types.RootElement, false);
        JsonElement PropertyType(string name) => Resolve(body.GetProperty("properties").GetProperty(name).GetProperty("type"), types.RootElement, false);
        var integer = PropertyType("integer");
        Assert.Equal(long.MinValue, integer.GetProperty("minValue").GetInt64());
        Assert.Equal(long.MaxValue, integer.GetProperty("maxValue").GetInt64());
        Assert.Equal("AnyType", PropertyType("any").GetProperty("$type").GetString());
        var outer = PropertyType("array");
        var array = Resolve(outer.GetProperty("elements")[0], types.RootElement, false);
        Assert.Equal(0, array.GetProperty("minLength").GetInt64());
        Assert.Equal(7, array.GetProperty("maxLength").GetInt64());
        Assert.Equal(PropertyType("map").GetProperty("additionalProperties").GetProperty("$ref").GetString(),
            array.GetProperty("itemType").GetProperty("$ref").GetString());
        Assert.Equal<string>(["Zulu", "Alpha"], PropertyType("enum").GetProperty("elements").EnumerateArray()
            .Select(element => Resolve(element, types.RootElement, false).GetProperty("value").GetString()!));
    }

    [Fact]
    public void ActualFrontendSample_HasResolvableRecursiveReferencesAndStableSourceOrder()
    {
        var sources = ContosoSampleFixture.ReadSources();
        TypeGenerationSerializedPackage Analyze((string Path, string Source)[] input)
        {
            var frontend = new TypeGenerationFrontend().Build(RoslynCompilationFactory.Create(input), new("Contoso", "1.2"));
            Assert.True(frontend.Succeeded);
            return Write(Lower(frontend.Model!));
        }
        var first = Analyze(sources);
        var second = Analyze([.. sources.Reverse()]);
        Assert.Equal<byte>(first.IndexJson, second.IndexJson);
        Assert.Equal<byte>(first.TypesJson, second.TypesJson);
        using var index = JsonDocument.Parse(first.IndexJson.ToArray());
        using var types = JsonDocument.Parse(first.TypesJson.ToArray());
        var resource = Resolve(index.RootElement.GetProperty("resources").GetProperty("Repository@2026-08-01"), types.RootElement, true);
        var body = Resolve(resource.GetProperty("body"), types.RootElement, false);
        var settingsReference = body.GetProperty("properties").GetProperty("settings").GetProperty("type");
        var settings = Resolve(settingsReference, types.RootElement, false);
        var settingsObject = Resolve(settings.GetProperty("elements")[0], types.RootElement, false);
        Assert.Equal(settingsReference.GetProperty("$ref").GetString(),
            settingsObject.GetProperty("properties").GetProperty("parent").GetProperty("type").GetProperty("$ref").GetString());
    }

    [Fact]
    public void BusinessNamesAndStringValues_AreNotRemovedByFormatMemberOmission()
    {
        var names = new[] { "resourceFunctions", "namespaceFunctions", "isPreview", "isDeprecated", "functions", "SourceLocation" };
        var body = Object("resourceFunctions", names.Select(name => Property(name, new StringLiteralTypeDefinition { Value = name })).ToArray());
        var result = Write(Lower(Model(Resource("namespaceFunctions", [], body))));
        using var index = JsonDocument.Parse(result.IndexJson.ToArray());
        using var types = JsonDocument.Parse(result.TypesJson.ToArray());
        var resource = Resolve(index.RootElement.GetProperty("resources").GetProperty("namespaceFunctions"), types.RootElement, true);
        var actual = Resolve(resource.GetProperty("body"), types.RootElement, false);
        Assert.Equal("resourceFunctions", actual.GetProperty("name").GetString());
        foreach (var name in names)
        {
            var property = actual.GetProperty("properties").GetProperty(name);
            Assert.Equal(name, Resolve(property.GetProperty("type"), types.RootElement, false).GetProperty("value").GetString());
        }
    }

    [Fact]
    public void UnicodeEscapingAndDiagnosticsOnlyLocations_DoNotChangeSemanticValues()
    {
        const string value = "引用 \"quoted\"\n\\path\t<&>";
        var body = Object("Object", Property(value, new StringLiteralTypeDefinition { Value = value })) with
        {
            SourceLocation = new() { FilePath = "UNWANTED_SOURCE_LOCATION_TOKEN.cs", Start = 123, Length = 4 },
        };
        var result = Write(Lower(Model(Resource(value, [], body))));
        using var index = JsonDocument.Parse(result.IndexJson.ToArray());
        using var types = JsonDocument.Parse(result.TypesJson.ToArray());
        var resource = Resolve(index.RootElement.GetProperty("resources").GetProperty(value), types.RootElement, true);
        Assert.Equal(value, resource.GetProperty("name").GetString());
        var actual = Resolve(resource.GetProperty("body"), types.RootElement, false);
        Assert.Equal(value, Resolve(actual.GetProperty("properties").GetProperty(value).GetProperty("type"), types.RootElement, false).GetProperty("value").GetString());
        Assert.DoesNotContain("UNWANTED_SOURCE_LOCATION_TOKEN", Encoding.UTF8.GetString(result.TypesJson.AsSpan()), StringComparison.Ordinal);
    }

    [Fact]
    public void SemanticInputOrderAndSourceLocations_DoNotAffectBytes()
    {
        var first = Minimal();
        var resource = first.ResourceTypes[0];
        var body = (ObjectTypeDefinition)resource.Body;
        var reorderedBody = body with { Properties = [.. body.Properties.Reverse()], SourceLocation = new() { FilePath = "other.cs", Start = 99, Length = 1 } };
        var reordered = first with { ResourceTypes = [resource with { Body = reorderedBody, ApiVersions = [.. resource.ApiVersions.Reverse()] }] };
        var expected = Write(Lower(first));
        var actual = Write(Lower(reordered));
        Assert.Equal<byte>(expected.IndexJson, actual.IndexJson);
        Assert.Equal<byte>(expected.TypesJson, actual.TypesJson);
    }

    [Fact]
    public void DifferentSharingTopologies_RemainSemanticallyEquivalentWithoutForcedDeduplication()
    {
        var text = new StringTypeDefinition();
        var shared = Write(Lower(Model(Resource("R", [], Object("Body", Property("a", text), Property("b", text))))));
        var separate = Write(Lower(Model(Resource("R", [], Object("Body", Property("a", new StringTypeDefinition()), Property("b", new StringTypeDefinition()))))));
        foreach (var package in new[] { shared, separate })
        {
            using var index = JsonDocument.Parse(package.IndexJson.ToArray());
            using var types = JsonDocument.Parse(package.TypesJson.ToArray());
            var body = Resolve(Resolve(index.RootElement.GetProperty("resources").GetProperty("R"), types.RootElement, true).GetProperty("body"), types.RootElement, false);
            foreach (var property in body.GetProperty("properties").EnumerateObject())
            {
                Assert.Equal("StringType", Resolve(property.Value.GetProperty("type"), types.RootElement, false).GetProperty("$type").GetString());
            }
        }
        using var sharedTypes = JsonDocument.Parse(shared.TypesJson.ToArray());
        using var separateTypes = JsonDocument.Parse(separate.TypesJson.ToArray());
        Assert.Equal(sharedTypes.RootElement.GetArrayLength() + 1, separateTypes.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task RepeatedConcurrentAndCultureVariantWrites_AreIsolated()
    {
        var models = new[] { Lower(Minimal()), Lower(ContosoSampleFixture.CreateExpected()) };
        var expected = models.Select(Write).ToArray();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => Task.Run(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(index % 2 == 0 ? "tr-TR" : "ar-SA");
            return (Index: index % 2, Result: Write(models[index % 2]));
        })));
        foreach (var result in results)
        {
            Assert.Equal<byte>(expected[result.Index].IndexJson, result.Result.IndexJson);
            Assert.Equal<byte>(expected[result.Index].TypesJson, result.Result.TypesJson);
        }
        using var stock = new MemoryStream();
        TypeSerializer.SerializeIndex(stock, models[0].Index);
        using var json = JsonDocument.Parse(stock.ToArray());
        Assert.True(json.RootElement.TryGetProperty("resourceFunctions", out _));
        Assert.True(json.RootElement.TryGetProperty("namespaceFunctions", out _));
    }

    [Fact]
    public void LegacyFalseDefaultsAndEmptyEmbeddedFunctions_AreOmittedWithoutMutatingModels()
    {
        var lowered = Lower(Minimal());
        var defaults = new TypeSettings("Contoso", "1.2", false);
        var index = new TypeIndex(lowered.Index.Resources, lowered.Index.ResourceFunctions, lowered.Index.NamespaceFunctions, defaults, null);
        var resourceIndex = lowered.Index.Resources["Repository@v1"].Index;
        var original = (ResourceType)lowered.Types[resourceIndex];
        var replacement = new ResourceType(original.Name, original.Body, new Dictionary<string, ResourceTypeFunction>(),
            writableScopes_in: original.WritableScopes, readableScopes_in: original.ReadableScopes);
        var result = Write(new(index, lowered.Types.SetItem(resourceIndex, replacement)));
        using var parsedIndex = JsonDocument.Parse(result.IndexJson.ToArray());
        using var parsedTypes = JsonDocument.Parse(result.TypesJson.ToArray());
        Assert.False(parsedIndex.RootElement.GetProperty("settings").TryGetProperty("isPreview", out _));
        Assert.False(parsedIndex.RootElement.GetProperty("settings").TryGetProperty("isDeprecated", out _));
        Assert.False(parsedTypes.RootElement[resourceIndex].TryGetProperty("functions", out _));
        Assert.False(defaults.IsPreview);
        Assert.False(defaults.IsDeprecated);
        Assert.NotNull(replacement.Functions);
    }

    [Theory]
    [InlineData("resource-functions")]
    [InlineData("namespace-functions")]
    [InlineData("preview")]
    [InlineData("deprecated")]
    [InlineData("fallback")]
    [InlineData("embedded-functions")]
    public void NonemptyOmittedSemantics_AreRejectedRatherThanDropped(string scenario)
    {
        var lowered = Lower(Minimal());
        var reference = lowered.Index.Resources.First().Value;
        var functions = scenario == "resource-functions"
            ? ImmutableDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>>.Empty.Add("Repository",
                ImmutableDictionary<string, IReadOnlyList<CrossFileTypeReference>>.Empty.Add("v1", [reference]))
            : lowered.Index.ResourceFunctions;
        var index = new TypeIndex(lowered.Index.Resources, functions,
            scenario == "namespace-functions" ? [reference] : lowered.Index.NamespaceFunctions,
            new("Contoso", "1.2", false, isPreview: scenario == "preview", isDeprecated: scenario == "deprecated"),
            scenario == "fallback" ? reference : null);
        var types = lowered.Types;
        if (scenario == "embedded-functions")
        {
            var resource = (ResourceType)types[reference.Index];
            types = types.SetItem(reference.Index, new ResourceType(resource.Name, resource.Body,
                new Dictionary<string, ResourceTypeFunction> { ["run"] = new(resource.Body, "Unsupported") },
                writableScopes_in: resource.WritableScopes, readableScopes_in: resource.ReadableScopes));
        }
        var result = TypeGenerationJsonWriter.Write(new(index, types));
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.Equal("TG2011", Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData("unhydrated")]
    [InlineData("external")]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("json-error")]
    public void SerializationFailures_NeverExposeTheAlreadySerializedIndex(string scenario)
    {
        var text = new StringType();
        ITypeReference reference = scenario == "unhydrated"
            ? new TypeFactory([]).GetReference(0)
            : new CallbackReference(text, () =>
            {
                if (scenario == "json-error") { throw new JsonException("Reference failure."); }
            });
        ImmutableArray<TypeBase> types = scenario switch
        {
            "duplicate" => [text, text],
            "unknown" => [new UnknownType()],
            _ => [new ObjectType("Invalid", new Dictionary<string, ObjectTypeProperty>
            {
                ["value"] = new(reference, ObjectTypePropertyFlags.None, null),
            }, null)],
        };
        var result = TypeGenerationJsonWriter.Write(new(Lower(Model()).Index, types));
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TG2301", diagnostic.Code);
        Assert.Equal(TypeGenDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("types.json", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal("TG2301", TypeGenDiagnosticCodes.BackendSerializationFailure);
    }

    [Fact]
    public void CancellationBeforeAndDuringSerialization_PropagatesWithoutPartialOutput()
    {
        Assert.Equal("model", Assert.Throws<ArgumentNullException>(() => TypeGenerationJsonWriter.Write(null!)).ParamName);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => TypeGenerationJsonWriter.Write(Lower(Model()), cancelled.Token));
        using var during = new CancellationTokenSource();
        var text = new StringType();
        var value = new ObjectType("Value", new Dictionary<string, ObjectTypeProperty>
        {
            ["text"] = new(new CallbackReference(text, during.Cancel), ObjectTypePropertyFlags.None, null),
        }, null);
        var model = new TypeGenerationLoweredPackage(Lower(Model()).Index, [text, value]);
        var error = Assert.Throws<OperationCanceledException>(() => TypeGenerationJsonWriter.Write(model, during.Token));
        Assert.Equal(during.Token, error.CancellationToken);
        Assert.True(TypeGenerationJsonWriter.Write(Lower(Model())).Succeeded);
    }

    [Fact]
    public void DeepAndCyclicGraphs_UseFlatReferencesInsteadOfSerializerObjectCycles()
    {
        TypeDefinition type = new BooleanTypeDefinition();
        for (var depth = 0; depth < 4096; depth++) { type = new ArrayTypeDefinition { ItemType = type }; }
        var result = Write(Lower(Model(Resource("R", [], Object("Body", Property("deep", type))))));
        using var types = JsonDocument.Parse(result.TypesJson.ToArray());
        Assert.Equal(4099, types.RootElement.GetArrayLength());
        var recursive = new ObjectTypeDefinition("Node", self => ([Property("self", self)], self));
        Write(Lower(Model(Resource("R", [], recursive))));
    }

    [Fact]
    public void InternalPayloadAndResultContracts_PreventPartialSuccess()
    {
        Assert.False(typeof(TypeGenerationJsonWriter).IsPublic);
        Assert.False(typeof(TypeGenerationSerializedPackage).IsPublic);
        Assert.Throws<ArgumentException>(() => new TypeGenerationSerializedPackage(default, [1]));
        Assert.Throws<ArgumentException>(() => new TypeGenerationSerializedPackage([], [1]));
        Assert.Throws<ArgumentException>(() => new TypeGenerationSerializedPackage([1], default));
        Assert.Throws<ArgumentException>(() => new TypeGenerationSerializedPackage([1], []));
        Assert.Throws<ArgumentException>(() => new TypeGenerationSerializationResult(null, default));
        Assert.Throws<ArgumentException>(() => new TypeGenerationSerializationResult(null, [null!]));
        var payload = new TypeGenerationSerializedPackage([1], [2]);
        Assert.Throws<ArgumentException>(() => new TypeGenerationSerializationResult(payload,
            [new("TG2301", TypeGenDiagnosticSeverity.Error, "Failure.", null)]));
        Assert.False(new TypeGenerationSerializationResult(null, []).Succeeded);
        Assert.True(new TypeGenerationSerializationResult(payload,
            [new("TG2301", TypeGenDiagnosticSeverity.Warning, "Example.", null)]).Succeeded);
    }

    private static TypeGenerationSerializedPackage Write(TypeGenerationLoweredPackage model)
    {
        var result = TypeGenerationJsonWriter.Write(model);
        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.Empty(result.Diagnostics);
        var package = Assert.IsType<TypeGenerationSerializedPackage>(result.Model);
        AssertCanonical(package);
        return package;
    }

    private static void AssertGolden(string name, ImmutableArray<byte> actual)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Snapshots", name);
        var expected = Encoding.UTF8.GetBytes(File.ReadAllText(path).ReplaceLineEndings("\n"));
        Assert.Equal<byte>(expected, actual);
    }

    private static void AssertCanonical(TypeGenerationSerializedPackage package)
    {
        foreach (var bytes in new[] { package.IndexJson, package.TypesJson })
        {
            Assert.False(bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }));
            Assert.DoesNotContain((byte)'\r', bytes);
            Assert.Equal((byte)'\n', bytes[^1]);
            Assert.DoesNotContain((byte)'\t', bytes);
        }
        using var index = JsonDocument.Parse(package.IndexJson.ToArray());
        using var types = JsonDocument.Parse(package.TypesJson.ToArray());
        Assert.Equal(JsonValueKind.Object, index.RootElement.ValueKind);
        Assert.Equal(JsonValueKind.Array, types.RootElement.ValueKind);
        Keys(index.RootElement, ["resources", "settings"], []);
        var settings = index.RootElement.GetProperty("settings");
        Keys(settings, ["name", "version", "isSingleton"], ["configurationType"]);
        Assert.Equal(JsonValueKind.String, settings.GetProperty("name").ValueKind);
        Assert.Equal(JsonValueKind.String, settings.GetProperty("version").ValueKind);
        Assert.Contains(settings.GetProperty("isSingleton").ValueKind, new[] { JsonValueKind.False, JsonValueKind.True });
        Assert.Equal(JsonValueKind.Object, index.RootElement.GetProperty("resources").ValueKind);
        foreach (var resource in index.RootElement.GetProperty("resources").EnumerateObject())
        {
            var target = Resolve(resource.Value, types.RootElement, true);
            Assert.Equal("ResourceType", target.GetProperty("$type").GetString());
            Assert.Equal(resource.Name, target.GetProperty("name").GetString());
        }
        if (settings.TryGetProperty("configurationType", out var configuration))
        {
            Assert.Equal("ObjectType", Resolve(configuration, types.RootElement, true).GetProperty("$type").GetString());
        }
        foreach (var type in types.RootElement.EnumerateArray())
        {
            switch (type.GetProperty("$type").GetString())
            {
                case "AnyType" or "NullType" or "BooleanType":
                    Keys(type, ["$type"], []);
                    break;
                case "IntegerType":
                    Keys(type, ["$type"], ["minValue", "maxValue"]);
                    break;
                case "StringType":
                    Keys(type, ["$type"], ["sensitive", "minLength", "maxLength", "pattern"]);
                    break;
                case "StringLiteralType":
                    Keys(type, ["$type", "value"], []);
                    Assert.Equal(JsonValueKind.String, type.GetProperty("value").ValueKind);
                    break;
                case "ArrayType":
                    Keys(type, ["$type", "itemType"], ["minLength", "maxLength"]);
                    Resolve(type.GetProperty("itemType"), types.RootElement, false);
                    break;
                case "UnionType":
                    Keys(type, ["$type", "elements"], []);
                    foreach (var element in type.GetProperty("elements").EnumerateArray()) { Resolve(element, types.RootElement, false); }
                    break;
                case "ObjectType":
                    Keys(type, ["$type", "name", "properties"], ["additionalProperties", "sensitive"]);
                    foreach (var property in type.GetProperty("properties").EnumerateObject())
                    {
                        Keys(property.Value, ["type", "flags"], ["description"]);
                        Assert.Equal(JsonValueKind.Number, property.Value.GetProperty("flags").ValueKind);
                        Resolve(property.Value.GetProperty("type"), types.RootElement, false);
                    }
                    if (type.TryGetProperty("additionalProperties", out var additional)) { Resolve(additional, types.RootElement, false); }
                    break;
                case "ResourceType":
                    Keys(type, ["$type", "name", "body", "readableScopes", "writableScopes"], []);
                    Assert.Equal(JsonValueKind.Number, type.GetProperty("readableScopes").ValueKind);
                    Assert.Equal(JsonValueKind.Number, type.GetProperty("writableScopes").ValueKind);
                    Assert.Equal("ObjectType", Resolve(type.GetProperty("body"), types.RootElement, false).GetProperty("$type").GetString());
                    break;
                default:
                    Assert.Fail($"Unexpected serialized kind: {type.GetProperty("$type").GetString()}");
                    break;
            }
        }
    }

    private static void Keys(JsonElement value, string[] required, string[] optional)
    {
        Assert.Equal(JsonValueKind.Object, value.ValueKind);
        foreach (var name in required) { Assert.True(value.TryGetProperty(name, out _), $"Missing {name}"); }
        foreach (var property in value.EnumerateObject())
        {
            Assert.Contains(property.Name, required.Concat(optional));
            Assert.NotEqual(JsonValueKind.Null, property.Value.ValueKind);
        }
    }

    private static JsonElement Resolve(JsonElement reference, JsonElement types, bool crossFile)
    {
        Assert.Equal("$ref", Assert.Single(reference.EnumerateObject()).Name);
        var value = reference.GetProperty("$ref").GetString()!;
        var prefix = crossFile ? "types.json#/" : "#/";
        Assert.StartsWith(prefix, value, StringComparison.Ordinal);
        var suffix = value[prefix.Length..];
        Assert.NotEmpty(suffix);
        Assert.All(suffix, character => Assert.InRange(character, '0', '9'));
        var index = int.Parse(suffix, CultureInfo.InvariantCulture);
        Assert.InRange(index, 0, types.GetArrayLength() - 1);
        return types[index];
    }

    private static TypeGenerationLoweredPackage Lower(ExtensionTypeSemanticModel source)
    {
        var projected = TypeGenerationProjectionService.Project(source);
        Assert.True(projected.Succeeded);
        var lowered = TypeGenerationLowerer.Lower(projected.Model!);
        Assert.True(lowered.Succeeded);
        return Assert.IsType<TypeGenerationLoweredPackage>(lowered.Model);
    }

    private static ExtensionTypeSemanticModel Minimal()
    {
        var text = new StringTypeDefinition();
        var nullable = Union(text, new NullTypeDefinition());
        var body = Object("Repository",
            Property("name", text) with { Flags = PropertyFlags.Required | PropertyFlags.Identifier, Description = "Repository name" },
            Property("description", nullable) with { Description = "Repository description" });
        return Model(Resource("Repository", ["v2", "v1"], body)) with
        {
            Configuration = Object("ContosoConfiguration", Property("organization", nullable)),
        };
    }

    private static ExtensionTypeSemanticModel Model(params ResourceTypeDefinition[] resources) =>
        new() { Name = "Contoso", Version = "1.2", ResourceTypes = [.. resources] };

    private static ResourceTypeDefinition Resource(string name, ImmutableArray<string> versions, ObjectTypeDefinition body) =>
        new() { Name = name, ApiVersions = versions, Body = body, ReadableScopes = ResourceScope.All, WritableScopes = ResourceScope.All, ResourceFunctions = [] };

    private static ObjectTypeDefinition Object(string name, params PropertyDefinition[] properties) =>
        new() { Name = name, Properties = [.. properties] };

    private static PropertyDefinition Property(string name, TypeDefinition type) => new() { Name = name, Type = type };

    private static UnionTypeDefinition Union(params TypeDefinition[] types) =>
        new() { Elements = [.. types.Select(type => new UnionElementDefinition { Type = type })] };

    private sealed class CallbackReference(TypeBase target, Action callback) : ITypeReference
    {
        public TypeBase Type
        {
            get
            {
                callback();
                return target;
            }
        }
    }

    private sealed class UnknownType : TypeBase;
}
