// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;

internal static class TypeGenerationJsonSemanticComparer
{
    public static ImmutableArray<TypeGenDiagnostic> Compare(
        TypeGenerationProjectedExtension expected,
        JsonElement index,
        JsonElement types,
        CancellationToken cancellationToken) =>
        new ComparisonContext(types, cancellationToken).Compare(expected, index);

    private sealed class ComparisonContext(JsonElement types, CancellationToken cancellationToken)
    {
        private readonly JsonElement[] nodes = types.EnumerateArray().ToArray();
        private readonly ImmutableArray<TypeGenDiagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<TypeGenDiagnostic>();
        private readonly Queue<(TypeDefinition Type, int Index, SourceLocation? Location)> pending = new();
        private readonly HashSet<(TypeDefinition Type, int Index)> visited = new(new PairComparer());

        public ImmutableArray<TypeGenDiagnostic> Compare(TypeGenerationProjectedExtension expected, JsonElement index)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var settings = index.GetProperty("settings");
            Check(settings.GetProperty("name").GetString() == expected.Name, "index.json#/settings/name", expected.SourceLocation);
            Check(settings.GetProperty("version").GetString() == expected.Version, "index.json#/settings/version", expected.SourceLocation);
            Check(settings.GetProperty("isSingleton").GetBoolean() == expected.IsSingleton, "index.json#/settings/isSingleton", expected.SourceLocation);
            var hasConfiguration = settings.TryGetProperty("configurationType", out var configuration);
            Check(hasConfiguration == (expected.Configuration is not null), "index.json#/settings/configurationType", expected.SourceLocation);
            if (hasConfiguration && expected.Configuration is { } config)
            {
                pending.Enqueue((config, ReferenceIndex(configuration), expected.SourceLocation));
            }

            var expectedResources = new Dictionary<string, ProjectedResourceTypeDefinition>(StringComparer.Ordinal);
            foreach (var resource in expected.ResourceTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var key = resource.ApiVersion is null ? resource.Name : $"{resource.Name}@{resource.ApiVersion}";
                Check(expectedResources.TryAdd(key, resource), "index.json#/resources/" + Segment(key), resource.SourceLocation ?? expected.SourceLocation);
            }
            var resources = index.GetProperty("resources");
            foreach (var entry in resources.EnumerateObject())
            {
                Check(expectedResources.ContainsKey(entry.Name), "index.json#/resources/" + Segment(entry.Name), expected.SourceLocation);
            }
            foreach (var (key, resource) in expectedResources.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var location = resource.SourceLocation ?? expected.SourceLocation;
                var present = resources.TryGetProperty(key, out var reference);
                Check(present, "index.json#/resources/" + Segment(key), location);
                if (!present) { continue; }
                var position = ReferenceIndex(reference);
                var target = nodes[position];
                var path = $"types.json#/{position}";
                Check(target.GetProperty("name").GetString() == key, path + "/name", location);
                Check(target.GetProperty("readableScopes").GetInt64() == (long)resource.ReadableScopes, path + "/readableScopes", location);
                Check(target.GetProperty("writableScopes").GetInt64() == (long)resource.WritableScopes, path + "/writableScopes", location);
                pending.Enqueue((resource.Body, ReferenceIndex(target.GetProperty("body")), location));
            }

            while (pending.TryDequeue(out var pair))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!visited.Add((pair.Type, pair.Index))) { continue; }
                CompareType(pair.Type, pair.Index, pair.Type.SourceLocation ?? pair.Location);
            }
            return diagnostics.ToImmutable();
        }

        private void CompareType(TypeDefinition expected, int position, SourceLocation? location)
        {
            var target = nodes[position];
            var path = $"types.json#/{position}";
            var kind = expected switch
            {
                AnyTypeDefinition => "AnyType",
                NullTypeDefinition => "NullType",
                BooleanTypeDefinition => "BooleanType",
                IntegerTypeDefinition => "IntegerType",
                StringTypeDefinition => "StringType",
                StringLiteralTypeDefinition => "StringLiteralType",
                ArrayTypeDefinition => "ArrayType",
                ObjectTypeDefinition => "ObjectType",
                UnionTypeDefinition => "UnionType",
                _ => throw new InvalidOperationException("The expected projection contains an unsupported type."),
            };
            if (target.GetProperty("$type").GetString() != kind)
            {
                Check(false, path + "/$type", location);
                return;
            }
            switch (expected)
            {
                case IntegerTypeDefinition integer:
                    Check(Number(target, "minValue") == integer.MinValue, path + "/minValue", location);
                    Check(Number(target, "maxValue") == integer.MaxValue, path + "/maxValue", location);
                    break;
                case StringTypeDefinition text:
                    Check(Boolean(target, "sensitive") == text.Sensitive, path + "/sensitive", location);
                    Check(Number(target, "minLength") == text.MinLength, path + "/minLength", location);
                    Check(Number(target, "maxLength") == text.MaxLength, path + "/maxLength", location);
                    Check(Text(target, "pattern") == text.Pattern, path + "/pattern", location);
                    break;
                case StringLiteralTypeDefinition literal:
                    Check(target.GetProperty("value").GetString() == literal.Value, path + "/value", location);
                    break;
                case ArrayTypeDefinition array:
                    Check(Number(target, "minLength") == array.MinLength, path + "/minLength", location);
                    Check(Number(target, "maxLength") == array.MaxLength, path + "/maxLength", location);
                    pending.Enqueue((array.ItemType, ReferenceIndex(target.GetProperty("itemType")), location));
                    break;
                case ObjectTypeDefinition obj:
                    CompareObject(obj, target, path, location);
                    break;
                case UnionTypeDefinition union:
                    var elements = target.GetProperty("elements");
                    if (elements.GetArrayLength() != union.Elements.Length)
                    {
                        Check(false, path + "/elements", location);
                        break;
                    }
                    for (var index = 0; index < union.Elements.Length; index++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        pending.Enqueue((union.Elements[index].Type, ReferenceIndex(elements[index]), union.Elements[index].SourceLocation ?? location));
                    }
                    break;
            }
        }

        private void CompareObject(ObjectTypeDefinition expected, JsonElement target, string path, SourceLocation? location)
        {
            Check(target.GetProperty("name").GetString() == expected.Name, path + "/name", location);
            Check(Boolean(target, "sensitive") == expected.Sensitive, path + "/sensitive", location);
            var hasAdditional = target.TryGetProperty("additionalProperties", out var additional);
            Check(hasAdditional == (expected.AdditionalProperties is not null), path + "/additionalProperties", location);
            if (hasAdditional && expected.AdditionalProperties is { } value)
            {
                pending.Enqueue((value, ReferenceIndex(additional), location));
            }

            var properties = target.GetProperty("properties");
            var names = expected.Properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var property in properties.EnumerateObject())
            {
                Check(names.Contains(property.Name), path + "/properties/" + Segment(property.Name), location);
            }
            foreach (var property in expected.Properties.OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var propertyLocation = property.SourceLocation ?? location;
                var propertyPath = path + "/properties/" + Segment(property.Name);
                var present = properties.TryGetProperty(property.Name, out var actual);
                Check(present, propertyPath, propertyLocation);
                if (!present) { continue; }
                Check(actual.GetProperty("flags").GetInt64() == (long)property.Flags, propertyPath + "/flags", propertyLocation);
                Check(Text(actual, "description") == property.Description, propertyPath + "/description", propertyLocation);
                pending.Enqueue((property.Type, ReferenceIndex(actual.GetProperty("type")), propertyLocation));
            }
        }

        private void Check(bool condition, string path, SourceLocation? location)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!condition)
            {
                diagnostics.Add(new(TypeGenDiagnosticCodes.BackendJsonSemanticMismatch, TypeGenDiagnosticSeverity.Error,
                    $"{path}: Serialized semantics do not match the expected projection.", location));
            }
        }
    }

    private sealed class PairComparer : IEqualityComparer<(TypeDefinition Type, int Index)>
    {
        public bool Equals((TypeDefinition Type, int Index) x, (TypeDefinition Type, int Index) y) =>
            ReferenceEquals(x.Type, y.Type) && x.Index == y.Index;

        public int GetHashCode((TypeDefinition Type, int Index) value) =>
            HashCode.Combine(RuntimeHelpers.GetHashCode(value.Type), value.Index);
    }

    private static string Segment(string value) => TypeGenerationPackageValidator.PointerSegment(value);

    private static long? Number(JsonElement node, string name) =>
        node.TryGetProperty(name, out var value) ? value.GetInt64() : null;

    private static string? Text(JsonElement node, string name) =>
        node.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static bool Boolean(JsonElement node, string name) =>
        node.TryGetProperty(name, out var value) && value.GetBoolean();

    private static int ReferenceIndex(JsonElement reference)
    {
        var value = reference.GetProperty("$ref").GetString()!;
        return int.Parse(value.AsSpan(value.LastIndexOf("#/", StringComparison.Ordinal) + 2), NumberStyles.None, CultureInfo.InvariantCulture);
    }
}
