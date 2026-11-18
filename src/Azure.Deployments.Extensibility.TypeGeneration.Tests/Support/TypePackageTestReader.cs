// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;

internal static class TypePackageTestReader
{
    public static (JsonElement Index, JsonElement Types) ReadArchive(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new TarReader(gzip);
        var index = ReadEntry("index.json");
        var types = ReadEntry("types.json");
        Assert.Null(reader.GetNextEntry());
        return ReadJson(index, types);

        byte[] ReadEntry(string name)
        {
            var entry = Assert.IsType<UstarTarEntry>(reader.GetNextEntry());
            Assert.Equal(name, entry.Name);
            Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
            Assert.Equal(DateTimeOffset.UnixEpoch, entry.ModificationTime);
            Assert.Equal(0, entry.Uid);
            Assert.Equal(0, entry.Gid);
            Assert.Equal("", entry.UserName);
            Assert.Equal("", entry.GroupName);
            Assert.Equal("", entry.LinkName);
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead, entry.Mode);
            using var data = new MemoryStream();
            Assert.NotNull(entry.DataStream);
            entry.DataStream.CopyTo(data);
            Assert.Equal(entry.Length, data.Length);
            return data.ToArray();
        }
    }

    public static (JsonElement Index, JsonElement Types) ReadJson(byte[] indexBytes, byte[] typeBytes)
    {
        foreach (var bytes in new[] { indexBytes, typeBytes })
        {
            Assert.False(bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }));
            Assert.DoesNotContain((byte)'\r', bytes);
            Assert.Equal((byte)'\n', bytes[^1]);
            Assert.DoesNotContain((byte)'\t', bytes);
            _ = new UTF8Encoding(false, true).GetString(bytes);
        }
        using var index = JsonDocument.Parse(indexBytes);
        using var types = JsonDocument.Parse(typeBytes);
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
                    Resolve(type.GetProperty("itemType"), types.RootElement);
                    break;
                case "UnionType":
                    Keys(type, ["$type", "elements"], []);
                    foreach (var element in type.GetProperty("elements").EnumerateArray()) { Resolve(element, types.RootElement); }
                    break;
                case "ObjectType":
                    Keys(type, ["$type", "name", "properties"], ["additionalProperties", "sensitive"]);
                    foreach (var property in type.GetProperty("properties").EnumerateObject())
                    {
                        Keys(property.Value, ["type", "flags"], ["description"]);
                        Assert.Equal(JsonValueKind.Number, property.Value.GetProperty("flags").ValueKind);
                        Resolve(property.Value.GetProperty("type"), types.RootElement);
                    }
                    if (type.TryGetProperty("additionalProperties", out var additional)) { Resolve(additional, types.RootElement); }
                    break;
                case "ResourceType":
                    Keys(type, ["$type", "name", "body", "readableScopes", "writableScopes"], []);
                    Assert.Equal(JsonValueKind.Number, type.GetProperty("readableScopes").ValueKind);
                    Assert.Equal(JsonValueKind.Number, type.GetProperty("writableScopes").ValueKind);
                    Assert.Equal("ObjectType", Resolve(type.GetProperty("body"), types.RootElement).GetProperty("$type").GetString());
                    break;
                default:
                    Assert.Fail($"Unexpected serialized kind: {type.GetProperty("$type").GetString()}");
                    break;
            }
        }
        return (index.RootElement.Clone(), types.RootElement.Clone());
    }

    public static JsonElement Resolve(JsonElement reference, JsonElement types, bool crossFile = false)
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

    private static void Keys(JsonElement value, string[] required, string[] optional)
    {
        Assert.Equal(JsonValueKind.Object, value.ValueKind);
        var names = value.EnumerateObject().Select(property => property.Name).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
        foreach (var name in required) { Assert.True(value.TryGetProperty(name, out _), $"Missing {name}"); }
        foreach (var property in value.EnumerateObject())
        {
            Assert.Contains(property.Name, required.Concat(optional));
            Assert.NotEqual(JsonValueKind.Null, property.Value.ValueKind);
        }
    }
}
