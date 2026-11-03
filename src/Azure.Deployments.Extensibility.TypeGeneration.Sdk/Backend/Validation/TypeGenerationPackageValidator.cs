// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;

internal static class TypeGenerationPackageValidator
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static TypeGenerationValidationResult Validate(
        TypeGenerationSerializedPackage package,
        TypeGenerationProjectedExtension? expected = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        cancellationToken.ThrowIfCancellationRequested();
        if (expected is not null)
        {
            var inputDiagnostics = TypeGenerationModelValidator.ValidateProjected(expected, cancellationToken);
            if (inputDiagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
            {
                return new(null, inputDiagnostics);
            }
        }

        var diagnostics = ImmutableArray.CreateBuilder<TypeGenDiagnostic>();
        using var index = Parse(package.IndexJson, TypeGenerationSerializedPackage.IndexFileName, diagnostics);
        cancellationToken.ThrowIfCancellationRequested();
        using var types = Parse(package.TypesJson, TypeGenerationSerializedPackage.TypesFileName, diagnostics);
        cancellationToken.ThrowIfCancellationRequested();
        if (index is not null && types is not null)
        {
            var context = new ValidationContext(index.RootElement, types.RootElement, diagnostics, cancellationToken);
            try
            {
                context.Validate();
                if (diagnostics.Count == 0 && expected is not null)
                {
                    diagnostics.AddRange(TypeGenerationJsonSemanticComparer.Compare(
                        expected, index.RootElement, types.RootElement, cancellationToken));
                }
            }
            catch (InvalidOperationException exception)
            {
                diagnostics.Add(new(TypeGenDiagnosticCodes.BackendInvalidJsonDocument, TypeGenDiagnosticSeverity.Error,
                    $"{context.CurrentPath}: Cannot read JSON values: {exception.Message}", null));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var ordered = TypeGenerationModelValidator.OrderDiagnostics(diagnostics);
        return new(ordered.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error)
            ? null : new TypeGenerationValidatedJsonPackage(package), ordered);
    }

    internal static string PointerSegment(string value) =>
        value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    private static JsonDocument? Parse(
        ImmutableArray<byte> bytes,
        string file,
        ImmutableArray<TypeGenDiagnostic>.Builder diagnostics)
    {
        try
        {
            StrictUtf8.GetCharCount(bytes.AsSpan());
            return JsonDocument.Parse(bytes.AsMemory());
        }
        catch (JsonException exception)
        {
            diagnostics.Add(new(TypeGenDiagnosticCodes.BackendInvalidJsonDocument, TypeGenDiagnosticSeverity.Error,
                $"{file}#: Invalid JSON: {exception.Message}", null));
        }
        catch (DecoderFallbackException exception)
        {
            diagnostics.Add(new(TypeGenDiagnosticCodes.BackendInvalidJsonDocument, TypeGenDiagnosticSeverity.Error,
                $"{file}#: Invalid UTF-8: {exception.Message}", null));
        }
        return null;
    }

    private enum ReferenceRole { Resource, Object, Value }

    private sealed class ValidationContext(
        JsonElement index,
        JsonElement types,
        ImmutableArray<TypeGenDiagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        private readonly List<(int Index, ReferenceRole Role, string Path, string? Name)> references = [];
        private JsonElement[] nodes = [];

        public string CurrentPath { get; private set; } = "index.json#";

        public void Validate()
        {
            var validIndex = Kind(index, JsonValueKind.Object, "index.json#");
            var validTypes = Kind(types, JsonValueKind.Array, "types.json#");
            if (!validIndex || !validTypes)
            {
                return;
            }
            nodes = types.EnumerateArray().ToArray();
            ValidateIndex();
            for (var position = 0; position < nodes.Length; position++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateType(nodes[position], $"types.json#/{position}");
            }
            if (diagnostics.Count != 0)
            {
                return;
            }
            foreach (var reference in references)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var target = nodes[reference.Index];
                var kind = target.GetProperty("$type").GetString();
                var compatible = reference.Role switch
                {
                    ReferenceRole.Resource => kind == "ResourceType",
                    ReferenceRole.Object => kind == "ObjectType",
                    _ => kind != "ResourceType",
                };
                if (!compatible)
                {
                    Error(TypeGenDiagnosticCodes.BackendInvalidJsonReference, reference.Path,
                        $"Reference target kind '{kind}' is incompatible with {reference.Role}.");
                }
                else if (reference.Name is not null && target.GetProperty("name").GetString() != reference.Name)
                {
                    Error(TypeGenDiagnosticCodes.BackendInvalidJsonValue, reference.Path,
                        $"Resource entry '{reference.Name}' does not match the target ResourceType.name.");
                }
            }
        }

        private void ValidateIndex()
        {
            if (!Members(index, "index.json#", ["resources", "settings"], []))
            {
                return;
            }
            var resources = index.GetProperty("resources");
            if (Dictionary(resources, "index.json#/resources"))
            {
                foreach (var resource in resources.EnumerateObject())
                {
                    var path = $"index.json#/resources/{PointerSegment(resource.Name)}";
                    Name(resource.Name, path);
                    Reference(resource.Value, path, true, ReferenceRole.Resource, resource.Name);
                }
            }
            var settings = index.GetProperty("settings");
            if (Members(settings, "index.json#/settings", ["name", "version", "isSingleton"], ["configurationType"]))
            {
                Text(settings.GetProperty("name"), "index.json#/settings/name", true);
                Text(settings.GetProperty("version"), "index.json#/settings/version", true);
                Boolean(settings.GetProperty("isSingleton"), "index.json#/settings/isSingleton");
                if (settings.TryGetProperty("configurationType", out var configuration))
                {
                    Reference(configuration, "index.json#/settings/configurationType", true, ReferenceRole.Object);
                }
            }
        }

        private void ValidateType(JsonElement node, string path)
        {
            if (!Kind(node, JsonValueKind.Object, path))
            {
                return;
            }
            if (!node.TryGetProperty("$type", out var discriminator) || discriminator.ValueKind != JsonValueKind.String)
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidJsonShape, path + "/$type", "A string $type discriminator is required.");
                return;
            }
            switch (discriminator.GetString())
            {
                case "AnyType" or "NullType" or "BooleanType":
                    Members(node, path, ["$type"], []);
                    break;
                case "IntegerType":
                    if (Members(node, path, ["$type"], ["minValue", "maxValue"]))
                    {
                        Bounds(node, path, "minValue", "maxValue", false);
                    }
                    break;
                case "StringType":
                    if (Members(node, path, ["$type"], ["sensitive", "minLength", "maxLength", "pattern"]))
                    {
                        Bounds(node, path, "minLength", "maxLength", true);
                        OptionalBoolean(node, "sensitive", path);
                        if (node.TryGetProperty("pattern", out var pattern)) { Text(pattern, path + "/pattern"); }
                    }
                    break;
                case "StringLiteralType":
                    if (Members(node, path, ["$type", "value"], [])) { Text(node.GetProperty("value"), path + "/value"); }
                    break;
                case "ArrayType":
                    if (Members(node, path, ["$type", "itemType"], ["minLength", "maxLength"]))
                    {
                        Bounds(node, path, "minLength", "maxLength", true);
                        Reference(node.GetProperty("itemType"), path + "/itemType", false, ReferenceRole.Value);
                    }
                    break;
                case "UnionType":
                    if (Members(node, path, ["$type", "elements"], []) && Kind(node.GetProperty("elements"), JsonValueKind.Array, path + "/elements"))
                    {
                        var elements = node.GetProperty("elements");
                        if (elements.GetArrayLength() == 0) { Error(TypeGenDiagnosticCodes.BackendInvalidJsonValue, path + "/elements", "A union must be nonempty."); }
                        var position = 0;
                        foreach (var element in elements.EnumerateArray())
                        {
                            Reference(element, $"{path}/elements/{position++}", false, ReferenceRole.Value);
                        }
                    }
                    break;
                case "ObjectType":
                    ValidateObject(node, path);
                    break;
                case "ResourceType":
                    if (Members(node, path, ["$type", "name", "body", "readableScopes", "writableScopes"], []))
                    {
                        Text(node.GetProperty("name"), path + "/name", true);
                        Flags(node.GetProperty("readableScopes"), path + "/readableScopes");
                        Flags(node.GetProperty("writableScopes"), path + "/writableScopes");
                        Reference(node.GetProperty("body"), path + "/body", false, ReferenceRole.Object);
                    }
                    break;
                default:
                    Error(TypeGenDiagnosticCodes.BackendInvalidJsonShape, path + "/$type",
                        $"Type kind '{discriminator.GetString()}' is not supported by this package contract.");
                    break;
            }
        }

        private void ValidateObject(JsonElement node, string path)
        {
            if (!Members(node, path, ["$type", "name", "properties"], ["additionalProperties", "sensitive"]))
            {
                return;
            }
            Text(node.GetProperty("name"), path + "/name", true);
            OptionalBoolean(node, "sensitive", path);
            if (node.TryGetProperty("additionalProperties", out var additional))
            {
                Reference(additional, path + "/additionalProperties", false, ReferenceRole.Value);
            }
            var properties = node.GetProperty("properties");
            if (!Dictionary(properties, path + "/properties"))
            {
                return;
            }
            foreach (var property in properties.EnumerateObject())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var propertyPath = path + "/properties/" + PointerSegment(property.Name);
                Name(property.Name, propertyPath);
                if (Members(property.Value, propertyPath, ["type", "flags"], ["description"]))
                {
                    Flags(property.Value.GetProperty("flags"), propertyPath + "/flags");
                    Reference(property.Value.GetProperty("type"), propertyPath + "/type", false, ReferenceRole.Value);
                    if (property.Value.TryGetProperty("description", out var description)) { Text(description, propertyPath + "/description"); }
                }
            }
        }

        private bool Members(JsonElement value, string path, string[] required, string[] optional)
        {
            if (!Dictionary(value, path))
            {
                return false;
            }
            var valid = true;
            foreach (var property in value.EnumerateObject())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!required.Contains(property.Name, StringComparer.Ordinal) && !optional.Contains(property.Name, StringComparer.Ordinal))
                {
                    valid = false;
                    Error(TypeGenDiagnosticCodes.BackendInvalidJsonShape, path + "/" + PointerSegment(property.Name), "Unknown or forbidden member.");
                }
                if (property.Value.ValueKind == JsonValueKind.Null)
                {
                    valid = false;
                    Error(TypeGenDiagnosticCodes.BackendInvalidJsonShape, path + "/" + PointerSegment(property.Name), "Null members must not be emitted.");
                }
            }
            foreach (var name in required)
            {
                if (!value.TryGetProperty(name, out _))
                {
                    valid = false;
                    Error(TypeGenDiagnosticCodes.BackendInvalidJsonShape, path + "/" + name, "Required member is missing.");
                }
            }
            return valid;
        }

        private bool Dictionary(JsonElement value, string path)
        {
            if (!Kind(value, JsonValueKind.Object, path))
            {
                return false;
            }
            var names = new HashSet<string>(StringComparer.Ordinal);
            var valid = true;
            foreach (var property in value.EnumerateObject())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!names.Add(property.Name))
                {
                    valid = false;
                    Error(TypeGenDiagnosticCodes.BackendInvalidJsonShape, path + "/" + PointerSegment(property.Name), "Duplicate JSON member.");
                }
            }
            return valid;
        }

        private void Reference(JsonElement value, string path, bool crossFile, ReferenceRole role, string? name = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CurrentPath = path;
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 1 ||
                !value.TryGetProperty("$ref", out var reference) || reference.ValueKind != JsonValueKind.String)
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidJsonReference, path, "A reference must contain exactly one string $ref member.");
                return;
            }
            var text = reference.GetString()!;
            var prefix = crossFile ? TypeGenerationSerializedPackage.TypesFileName + "#/" : "#/";
            if (!text.StartsWith(prefix, StringComparison.Ordinal))
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidJsonReference, path, $"Reference must use '{prefix}N'; the placement or target file is invalid.");
                return;
            }
            var suffix = text.AsSpan(prefix.Length);
            var digits = !suffix.IsEmpty;
            foreach (var character in suffix)
            {
                if (character is < '0' or > '9') { digits = false; break; }
            }
            if (!digits || !int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var position) ||
                position < 0 || position >= nodes.Length)
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidJsonReference, path, $"Reference index in '{text}' is invalid or out of range.");
                return;
            }
            references.Add((position, role, path, name));
        }

        private void Bounds(JsonElement value, string path, string minName, string maxName, bool nonnegative)
        {
            var min = value.TryGetProperty(minName, out var minElement) ? Integer(minElement, path + "/" + minName) : null;
            var max = value.TryGetProperty(maxName, out var maxElement) ? Integer(maxElement, path + "/" + maxName) : null;
            if ((nonnegative && (min < 0 || max < 0)) || (min.HasValue && max.HasValue && min > max))
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidJsonValue, path, "Bounds must be ordered and lengths must be nonnegative.");
            }
        }

        private long? Integer(JsonElement value, string path)
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            {
                return number;
            }
            Error(TypeGenDiagnosticCodes.BackendInvalidJsonShape, path, "A signed 64-bit JSON integer is required.");
            return null;
        }

        private void Flags(JsonElement value, string path)
        {
            if (Integer(value, path) is { } bits && (bits < 0 || (bits & ~31L) != 0))
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidJsonValue, path, "Only the defined flag/scope bits 0 through 31 are allowed.");
            }
        }

        private void OptionalBoolean(JsonElement value, string name, string path)
        {
            if (value.TryGetProperty(name, out var member)) { Boolean(member, path + "/" + name); }
        }

        private void Boolean(JsonElement value, string path)
        {
            if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidJsonShape, path, "A JSON boolean is required.");
            }
        }

        private void Text(JsonElement value, string path, bool nonblank = false)
        {
            if (Kind(value, JsonValueKind.String, path) && nonblank) { Name(value.GetString()!, path); }
            else if (value.ValueKind == JsonValueKind.String) { _ = value.GetString(); }
        }

        private void Name(string name, string path)
        {
            if (string.IsNullOrWhiteSpace(name)) { Error(TypeGenDiagnosticCodes.BackendInvalidJsonValue, path, "A nonblank name/version is required."); }
        }

        private bool Kind(JsonElement value, JsonValueKind expected, string path)
        {
            CurrentPath = path;
            if (value.ValueKind == expected) { return true; }
            Error(TypeGenDiagnosticCodes.BackendInvalidJsonShape, path, $"Expected JSON {expected}, found {value.ValueKind}.");
            return false;
        }

        private void Error(string code, string path, string message)
        {
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Add(new(code, TypeGenDiagnosticSeverity.Error, $"{path}: {message}", null));
        }
    }
}
