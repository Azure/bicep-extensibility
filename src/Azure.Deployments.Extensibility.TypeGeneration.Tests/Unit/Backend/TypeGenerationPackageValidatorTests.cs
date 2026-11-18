// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Lowering;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Backend;

[Trait("Category", "Unit")]
public class TypeGenerationPackageValidatorTests
{
    [Fact]
    public void HandwrittenGolden_IsValidatedWithoutProducerObjects()
    {
        var files = Golden();
        var result = TypeGenerationPackageValidator.Validate(files);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Same(files, result.Model!.Files);
    }

    [Theory]
    [InlineData("{", true, "TG2401")]
    [InlineData("[", false, "TG2401")]
    [InlineData("[]", true, "TG2402")]
    [InlineData("{}", false, "TG2402")]
    [InlineData("null", true, "TG2402")]
    [InlineData("null", false, "TG2402")]
    [InlineData("{ /* comment */ }", true, "TG2401")]
    [InlineData("[{},]", false, "TG2401")]
    public void InvalidDocuments_ReturnFileContext(string json, bool index, string code)
    {
        var original = Golden();
        var files = new TypeGenerationSerializedPackage(index ? Bytes(json) : original.IndexJson, index ? original.TypesJson : Bytes(json));
        var result = Failure(files, code);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message.Contains(index ? "index.json" : "types.json", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidUtf8_IsRejected(bool index)
    {
        var original = Golden();
        var files = new TypeGenerationSerializedPackage(index ? [0xff] : original.IndexJson, index ? original.TypesJson : [0xff]);
        Failure(files, "TG2401");
    }

    [Fact]
    public void InvalidEscapedUnicode_DoesNotEscapeAsAnException()
    {
        var files = Golden();
        var json = """{"resources":{},"settings":{"name":"\uD800","version":"1","isSingleton":false}}""";
        var result = Failure(new(Bytes(json), files.TypesJson), "TG2401");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message.Contains("index.json", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("extra-root")]
    [InlineData("resource-functions")]
    [InlineData("namespace-functions")]
    [InlineData("fallback")]
    [InlineData("missing-resources")]
    [InlineData("null-resources")]
    [InlineData("resources-array")]
    [InlineData("missing-settings")]
    [InlineData("missing-name")]
    [InlineData("missing-version")]
    [InlineData("missing-singleton")]
    [InlineData("null-singleton")]
    [InlineData("numeric-singleton")]
    [InlineData("preview")]
    [InlineData("deprecated")]
    [InlineData("null-configuration")]
    [InlineData("missing-kind")]
    [InlineData("unknown-kind")]
    [InlineData("numeric-kind")]
    [InlineData("extra-type-field")]
    [InlineData("null-optional")]
    [InlineData("missing-body")]
    [InlineData("missing-scope")]
    [InlineData("legacy-scope")]
    [InlineData("legacy-flags")]
    [InlineData("embedded-functions")]
    [InlineData("missing-properties")]
    [InlineData("null-properties")]
    [InlineData("missing-property-type")]
    [InlineData("missing-property-flags")]
    [InlineData("null-description")]
    [InlineData("extra-property-field")]
    [InlineData("array-no-item")]
    [InlineData("union-no-elements")]
    [InlineData("union-wrong-elements")]
    [InlineData("literal-wrong-value")]
    public void OutputShapeMutations_AreRejected(string scenario)
    {
        var files = Mutate(Golden(), (index, types) =>
        {
            var settings = index["settings"]!.AsObject();
            var resource = types[5]!.AsObject();
            var body = types[4]!.AsObject();
            var property = body["properties"]!["name"]!.AsObject();
            switch (scenario)
            {
                case "extra-root": index["extra"] = true; break;
                case "resource-functions": index["resourceFunctions"] = new JsonObject(); break;
                case "namespace-functions": index["namespaceFunctions"] = new JsonArray(); break;
                case "fallback": index["fallbackResourceType"] = Ref("types.json#/5"); break;
                case "missing-resources": index.Remove("resources"); break;
                case "null-resources": index["resources"] = null; break;
                case "resources-array": index["resources"] = new JsonArray(); break;
                case "missing-settings": index.Remove("settings"); break;
                case "missing-name": settings.Remove("name"); break;
                case "missing-version": settings.Remove("version"); break;
                case "missing-singleton": settings.Remove("isSingleton"); break;
                case "null-singleton": settings["isSingleton"] = null; break;
                case "numeric-singleton": settings["isSingleton"] = 0; break;
                case "preview": settings["isPreview"] = false; break;
                case "deprecated": settings["isDeprecated"] = false; break;
                case "null-configuration": settings["configurationType"] = null; break;
                case "missing-kind": types[2]!.AsObject().Remove("$type"); break;
                case "unknown-kind": types[2]!["$type"] = "BuiltInType"; break;
                case "numeric-kind": types[2]!["$type"] = 1; break;
                case "extra-type-field": types[2]!["resourceFunctions"] = new JsonObject(); break;
                case "null-optional": types[2]!["pattern"] = null; break;
                case "missing-body": resource.Remove("body"); break;
                case "missing-scope": resource.Remove("readableScopes"); break;
                case "legacy-scope": resource["scopeType"] = 31; break;
                case "legacy-flags": resource["flags"] = 0; break;
                case "embedded-functions": resource["functions"] = new JsonObject(); break;
                case "missing-properties": body.Remove("properties"); break;
                case "null-properties": body["properties"] = null; break;
                case "missing-property-type": property.Remove("type"); break;
                case "missing-property-flags": property.Remove("flags"); break;
                case "null-description": property["description"] = null; break;
                case "extra-property-field": property["nullable"] = true; break;
                case "array-no-item": types[2] = new JsonObject { ["$type"] = "ArrayType" }; break;
                case "union-no-elements": types[1]!.AsObject().Remove("elements"); break;
                case "union-wrong-elements": types[1]!["elements"] = new JsonObject(); break;
                case "literal-wrong-value": types[2] = new JsonObject { ["$type"] = "StringLiteralType", ["value"] = 0 }; break;
            }
        });
        Failure(files, "TG2402");
    }

    [Theory]
    [InlineData("root")]
    [InlineData("resource")]
    [InlineData("type")]
    [InlineData("property")]
    public void DuplicateJsonMembers_AreNotAcceptedThroughLastValueWins(string scenario)
    {
        var original = Golden();
        var index = Encoding.UTF8.GetString(original.IndexJson.AsSpan());
        var types = Encoding.UTF8.GetString(original.TypesJson.AsSpan());
        switch (scenario)
        {
            case "root":
                index = index.Replace("\"settings\": {", "\"resources\": {}, \"settings\": {", StringComparison.Ordinal);
                break;
            case "resource":
                index = index.Replace("\"Repository@v1\": {", "\"Repository@v1\": {\"$ref\":\"types.json#/5\"}, \"Repository@v1\": {", StringComparison.Ordinal);
                break;
            case "type":
                types = types.Replace("\"name\": \"Repository\"", "\"name\": \"ignored\", \"name\": \"Repository\"", StringComparison.Ordinal);
                break;
            case "property":
                types = types.Replace("\"flags\": 17", "\"flags\": 0, \"flags\": 17", StringComparison.Ordinal);
                break;
        }
        Failure(new(Bytes(index), Bytes(types)), "TG2402");
    }

    [Theory]
    [InlineData("#/5")]
    [InlineData("missing.json#/5")]
    [InlineData("/types.json#/5")]
    [InlineData("../types.json#/5")]
    [InlineData("types.json#/-1")]
    [InlineData("types.json#/2147483648")]
    [InlineData("types.json#/")]
    [InlineData("types.json#/1.0")]
    [InlineData("types.json#/+5")]
    [InlineData("types.json#/ 5")]
    [InlineData("types.json#/5\0")]
    [InlineData("types.json#/999")]
    [InlineData("types.json\\#/5")]
    [InlineData("types.json#/٥")]
    public void CrossFileReferenceSyntaxAndClosure_AreEnforced(string reference)
    {
        Failure(Mutate(Golden(), (index, _) => index["resources"]!["Repository@v1"]!["$ref"] = reference), "TG2403");
    }

    [Theory]
    [InlineData("property")]
    [InlineData("array")]
    [InlineData("union")]
    [InlineData("additional")]
    [InlineData("configuration")]
    public void ReferencePlacement_IsCheckedAtEverySupportedEdge(string scenario)
    {
        Failure(Mutate(Golden(), (index, types) =>
        {
            switch (scenario)
            {
                case "property": types[4]!["properties"]!["name"]!["type"] = Ref("types.json#/2"); break;
                case "array": types[2] = new JsonObject { ["$type"] = "ArrayType", ["itemType"] = Ref("types.json#/3") }; break;
                case "union": types[1]!["elements"]![0] = Ref("types.json#/2"); break;
                case "additional": types[4]!["additionalProperties"] = Ref("types.json#/2"); break;
                case "configuration": index["settings"]!["configurationType"] = Ref("#/0"); break;
            }
        }), "TG2403");
    }

    [Theory]
    [InlineData("string")]
    [InlineData("null-value")]
    [InlineData("extra-member")]
    [InlineData("number")]
    public void ReferenceObjects_HaveExactlyOneStringRef(string scenario)
    {
        Failure(Mutate(Golden(), (index, _) =>
        {
            index["resources"]!["Repository@v1"] = scenario switch
            {
                "string" => JsonValue.Create("types.json#/5"),
                "null-value" => new JsonObject { ["$ref"] = null },
                "number" => new JsonObject { ["$ref"] = 5 },
                _ => new JsonObject { ["$ref"] = "types.json#/5", ["extra"] = true },
            };
        }), "TG2403");
    }

    [Theory]
    [InlineData("resource")]
    [InlineData("configuration")]
    [InlineData("body")]
    [InlineData("property")]
    [InlineData("union")]
    [InlineData("array")]
    [InlineData("additional")]
    public void ReferenceTargetKinds_AreNotJustBoundsChecks(string scenario)
    {
        Failure(Mutate(Golden(), (index, types) =>
        {
            switch (scenario)
            {
                case "resource": index["resources"]!["Repository@v1"] = Ref("types.json#/2"); break;
                case "configuration": index["settings"]!["configurationType"] = Ref("types.json#/5"); break;
                case "body": types[5]!["body"] = Ref("#/2"); break;
                case "property": types[4]!["properties"]!["name"]!["type"] = Ref("#/5"); break;
                case "union": types[1]!["elements"]![0] = Ref("#/5"); break;
                case "array": types[2] = new JsonObject { ["$type"] = "ArrayType", ["itemType"] = Ref("#/5") }; break;
                case "additional": types[4]!["additionalProperties"] = Ref("#/5"); break;
            }
        }), "TG2403");
    }

    [Theory]
    [InlineData("flags-negative", "TG2404")]
    [InlineData("flags-unknown", "TG2404")]
    [InlineData("scope-negative", "TG2404")]
    [InlineData("scope-unknown", "TG2404")]
    [InlineData("flags-fraction", "TG2402")]
    [InlineData("scope-string", "TG2402")]
    [InlineData("integer-overflow", "TG2402")]
    [InlineData("length-negative", "TG2404")]
    [InlineData("length-reversed", "TG2404")]
    [InlineData("integer-reversed", "TG2404")]
    [InlineData("union-empty", "TG2404")]
    [InlineData("blank-extension", "TG2404")]
    [InlineData("blank-version", "TG2404")]
    [InlineData("blank-object", "TG2404")]
    [InlineData("resource-name-mismatch", "TG2404")]
    public void InvalidValues_AreDiagnosed(string scenario, string code)
    {
        Failure(Mutate(Golden(), (index, types) =>
        {
            switch (scenario)
            {
                case "flags-negative": types[4]!["properties"]!["name"]!["flags"] = -1; break;
                case "flags-unknown": types[4]!["properties"]!["name"]!["flags"] = 32; break;
                case "scope-negative": types[5]!["readableScopes"] = -1; break;
                case "scope-unknown": types[5]!["writableScopes"] = 32; break;
                case "flags-fraction": types[4]!["properties"]!["name"]!["flags"] = 1.5; break;
                case "scope-string": types[5]!["readableScopes"] = "31"; break;
                case "integer-overflow": types[2] = JsonNode.Parse("""{"$type":"IntegerType","maxValue":18446744073709551615}"""); break;
                case "length-negative": types[2]!["minLength"] = -1; break;
                case "length-reversed": types[2]!["minLength"] = 2; types[2]!["maxLength"] = 1; break;
                case "integer-reversed": types[2] = new JsonObject { ["$type"] = "IntegerType", ["minValue"] = 2, ["maxValue"] = 1 }; break;
                case "union-empty": types[1]!["elements"] = new JsonArray(); break;
                case "blank-extension": index["settings"]!["name"] = " "; break;
                case "blank-version": index["settings"]!["version"] = ""; break;
                case "blank-object": types[4]!["name"] = ""; break;
                case "resource-name-mismatch": types[5]!["name"] = "Other"; break;
            }
        }), code);
    }

    [Theory]
    [InlineData("extension-name")]
    [InlineData("extension-version")]
    [InlineData("singleton")]
    [InlineData("configuration-remove")]
    [InlineData("configuration-target")]
    [InlineData("resource-remove")]
    [InlineData("resource-add")]
    [InlineData("body-target")]
    [InlineData("readable-scopes")]
    [InlineData("writable-scopes")]
    [InlineData("property-remove")]
    [InlineData("property-add")]
    [InlineData("property-flags")]
    [InlineData("property-description")]
    [InlineData("property-type")]
    [InlineData("object-name")]
    [InlineData("object-sensitive")]
    [InlineData("string-pattern")]
    [InlineData("string-sensitive")]
    [InlineData("nullable-order")]
    [InlineData("array-bound")]
    [InlineData("enum-value")]
    [InlineData("map-value")]
    [InlineData("integer-bound")]
    public void WellFormedButChangedSemantics_FailExpectedProjectionComparison(string scenario)
    {
        var source = RichModel();
        var (expected, files) = Prepare(source);
        var mutated = Mutate(files, (index, types) =>
        {
            var resources = index["resources"]!.AsObject();
            var firstKey = resources.First().Key;
            var resource = Resolve(types, resources[firstKey]!);
            var body = Resolve(types, resource["body"]!);
            var properties = body["properties"]!.AsObject();
            var text = Resolve(types, properties["name"]!["type"]!);
            var nullable = Resolve(types, properties["description"]!["type"]!);
            var settingsUnion = Resolve(types, properties["settings"]!["type"]!);
            var settings = Resolve(types, settingsUnion["elements"]![0]!);
            var settingsProperties = settings["properties"]!.AsObject();
            switch (scenario)
            {
                case "extension-name": index["settings"]!["name"] = "Other"; break;
                case "extension-version": index["settings"]!["version"] = "Other"; break;
                case "singleton": index["settings"]!["isSingleton"] = true; break;
                case "configuration-remove": index["settings"]!.AsObject().Remove("configurationType"); break;
                case "configuration-target":
                    index["settings"]!["configurationType"] = Ref("types.json" + resource["body"]!["$ref"]!.GetValue<string>());
                    break;
                case "resource-remove": resources.Remove(firstKey); break;
                case "resource-add":
                    var extra = resource.DeepClone().AsObject();
                    extra["name"] = "Extra";
                    resources["Extra"] = Ref($"types.json#/{types.Count}");
                    types.Add(extra);
                    break;
                case "body-target":
                    resource["body"] = Ref("#/" + ReferenceIndex(index["settings"]!["configurationType"]!));
                    break;
                case "readable-scopes": resource["readableScopes"] = 1; break;
                case "writable-scopes": resource["writableScopes"] = 0; break;
                case "property-remove": properties.Remove("url"); break;
                case "property-add": properties["extra"] = new JsonObject { ["type"] = properties["name"]!["type"]!.DeepClone(), ["flags"] = 0 }; break;
                case "property-flags": properties["name"]!["flags"] = 16; break;
                case "property-description": properties["name"]!["description"] = "Changed"; break;
                case "property-type": properties["name"]!["type"] = properties["count"]!["type"]!.DeepClone(); break;
                case "object-name": body["name"] = "Changed"; break;
                case "object-sensitive": body["sensitive"] = true; break;
                case "string-pattern": text["pattern"] = "^changed$"; break;
                case "string-sensitive": text["sensitive"] = true; break;
                case "nullable-order":
                    var elements = nullable["elements"]!.AsArray();
                    var first = elements[0]!.DeepClone();
                    elements[0] = elements[1]!.DeepClone();
                    elements[1] = first;
                    break;
                case "array-bound": Resolve(types, settingsProperties["topics"]!["type"]!)["maxLength"] = 7; break;
                case "enum-value":
                    var union = Resolve(types, settingsProperties["visibility"]!["type"]!);
                    Resolve(types, union["elements"]![0]!)["value"] = "Changed";
                    break;
                case "map-value":
                    Resolve(types, settingsProperties["labels"]!["type"]!)["additionalProperties"] = properties["count"]!["type"]!.DeepClone();
                    break;
                case "integer-bound": Resolve(types, properties["count"]!["type"]!)["maxValue"] = 99; break;
            }
        });
        Assert.True(TypeGenerationPackageValidator.Validate(mutated).Succeeded);
        Failure(mutated, "TG2405", expected);
    }

    [Fact]
    public void SemanticMismatch_RetainsExpectedPropertyLocationAndEscapedJsonPointer()
    {
        var location = new SourceLocation { FilePath = "model.cs", Start = 4, Length = 2 };
        var body = Object("Body", Property("a/b~c", new BooleanTypeDefinition()) with { SourceLocation = location });
        var (expected, files) = Prepare(Model(Resource("R", [], body)));
        var changed = Mutate(files, (index, types) =>
        {
            var resource = Resolve(types, index["resources"]!["R"]!);
            Resolve(types, resource["body"]!)["properties"]!["a/b~c"]!["flags"] = 1;
        });
        var error = Assert.Single(Failure(changed, "TG2405", expected).Diagnostics);
        Assert.Same(location, error.SourceLocation);
        Assert.Contains("a~1b~0c/flags", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedAndDuplicatedEquivalentGraphs_MatchTheSameExpectedSemantics()
    {
        var text = new StringTypeDefinition();
        var (expected, _) = Prepare(Model(Resource("R", [], Object("Body", Property("a", text), Property("b", text)))));
        var (_, separate) = Prepare(Model(Resource("R", [], Object("Body",
            Property("a", new StringTypeDefinition()), Property("b", new StringTypeDefinition())))));
        Assert.True(TypeGenerationPackageValidator.Validate(separate, expected).Succeeded);
    }

    [Fact]
    public void SourceLocationsAndPropertyOrder_DoNotBecomeSemanticIdentity()
    {
        var (expected, files) = Prepare(RichModel());
        var resource = expected.ResourceTypes[0];
        var body = (ObjectTypeDefinition)resource.Body;
        var reordered = new TypeGenerationProjectedExtension(RichModel(),
            [.. expected.ResourceTypes.Select(value => value with
            {
                Body = body with { Properties = [.. body.Properties.Reverse()], SourceLocation = new() { FilePath = "different.cs", Start = 42, Length = 2 } },
            })]);
        Assert.True(TypeGenerationPackageValidator.Validate(files, reordered).Succeeded);
    }

    [Fact]
    public async Task RepeatedConcurrentValidation_IsReadOnlyAndDeterministic()
    {
        var (expected, original) = Prepare(RichModel());
        var bad = Mutate(original, (index, types) =>
        {
            index["settings"]!["extra"] = true;
            types[0]!["extra"] = true;
        });
        var first = Failure(bad, "TG2402");
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() => TypeGenerationPackageValidator.Validate(bad))));
        Assert.All(results, result => Assert.Equal<TypeGenDiagnostic>(first.Diagnostics, result.Diagnostics));
        var success = TypeGenerationPackageValidator.Validate(original, expected);
        Assert.True(success.Succeeded);
        Assert.Same(original, success.Model!.Files);
    }

    [Fact]
    public void NullArgumentsCancellationAndInvalidExpectedInput_AreExplicit()
    {
        Assert.Equal("package", Assert.Throws<ArgumentNullException>(() => TypeGenerationPackageValidator.Validate(null!)).ParamName);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => TypeGenerationPackageValidator.Validate(Golden(), cancellationToken: cancellation.Token));
        var expected = new TypeGenerationProjectedExtension(Model() with { Name = "" }, []);
        Failure(Golden(), "TG2001", expected);
    }

    [Fact]
    public void ValidationResult_RejectsMalformedDiagnosticsAndErrorProofs()
    {
        var validated = TypeGenerationPackageValidator.Validate(Golden()).Model!;
        Assert.Throws<ArgumentNullException>(() => new TypeGenerationValidatedJsonPackage(null!));
        Assert.Throws<ArgumentException>(() => new TypeGenerationValidationResult(null, default));
        Assert.Throws<ArgumentException>(() => new TypeGenerationValidationResult(null, [null!]));
        Assert.Throws<ArgumentException>(() => new TypeGenerationValidationResult(validated,
            [new("TG2401", TypeGenDiagnosticSeverity.Error, "Failure.", null)]));
        Assert.False(new TypeGenerationValidationResult(null, []).Succeeded);
        Assert.True(new TypeGenerationValidationResult(validated,
            [new("TG2401", TypeGenDiagnosticSeverity.Warning, "Example.", null)]).Succeeded);
    }

    private static TypeGenerationValidationResult Failure(
        TypeGenerationSerializedPackage files,
        string code,
        TypeGenerationProjectedExtension? expected = null)
    {
        var result = TypeGenerationPackageValidator.Validate(files, expected);
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
        return result;
    }

    private static TypeGenerationSerializedPackage Golden()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Snapshots");
        return new(Bytes(File.ReadAllText(Path.Combine(root, "MinimalPackage.index.json"))),
            Bytes(File.ReadAllText(Path.Combine(root, "MinimalPackage.types.json"))));
    }

    private static TypeGenerationSerializedPackage Mutate(TypeGenerationSerializedPackage files, Action<JsonObject, JsonArray> mutation)
    {
        var index = JsonNode.Parse(files.IndexJson.AsSpan())!.AsObject();
        var types = JsonNode.Parse(files.TypesJson.AsSpan())!.AsArray();
        mutation(index, types);
        return new(Bytes(index.ToJsonString()), Bytes(types.ToJsonString()));
    }

    private static (TypeGenerationProjectedExtension Expected, TypeGenerationSerializedPackage Files) Prepare(ExtensionTypeSemanticModel source)
    {
        var projection = TypeGenerationProjectionService.Project(source);
        Assert.True(projection.Succeeded);
        var lowered = TypeGenerationLowerer.Lower(projection.Model!);
        Assert.True(lowered.Succeeded);
        var written = TypeGenerationJsonWriter.Write(lowered.Model!);
        Assert.True(written.Succeeded);
        return (projection.Model!, written.Model!);
    }

    private static ExtensionTypeSemanticModel RichModel()
    {
        var model = ContosoSampleFixture.CreateExpected();
        var resource = model.ResourceTypes[0];
        var body = (ObjectTypeDefinition)resource.Body;
        return model with
        {
            ResourceTypes = [resource with
            {
                Body = body with { Properties = body.Properties.Add(Property("count", new IntegerTypeDefinition { MinValue = 0, MaxValue = 100 })) },
            }],
        };
    }

    private static JsonObject Resolve(JsonArray types, JsonNode reference) => types[ReferenceIndex(reference)]!.AsObject();

    private static int ReferenceIndex(JsonNode reference)
    {
        var value = reference["$ref"]!.GetValue<string>();
        return int.Parse(value[(value.LastIndexOf("#/", StringComparison.Ordinal) + 2)..], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static JsonObject Ref(string value) => new() { ["$ref"] = value };

    private static ImmutableArray<byte> Bytes(string value) => Encoding.UTF8.GetBytes(value).ToImmutableArray();

    private static ExtensionTypeSemanticModel Model(params ResourceTypeDefinition[] resources) =>
        new() { Name = "Contoso", Version = "1.2", ResourceTypes = [.. resources] };

    private static ResourceTypeDefinition Resource(string name, ImmutableArray<string> versions, ObjectTypeDefinition body) =>
        new() { Name = name, ApiVersions = versions, Body = body, ReadableScopes = ResourceScope.All, WritableScopes = ResourceScope.All, ResourceFunctions = [] };

    private static ObjectTypeDefinition Object(string name, params PropertyDefinition[] properties) =>
        new() { Name = name, Properties = [.. properties] };

    private static PropertyDefinition Property(string name, TypeDefinition type) => new() { Name = name, Type = type };
}
