// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Lowering;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;

internal static class TypeGenerationJsonWriter
{
    public static TypeGenerationSerializationResult Write(
        TypeGenerationLoweredPackage model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = CheckOmittedSemantics(model, cancellationToken);
        if (!diagnostics.IsEmpty)
        {
            return new(null, diagnostics);
        }

        var file = TypeGenerationSerializedPackage.IndexFileName;
        try
        {
            var index = Serialize(model.Index, Options());
            cancellationToken.ThrowIfCancellationRequested();
            file = TypeGenerationSerializedPackage.TypesFileName;
            var types = model.Types.ToArray();
            var serializedTypes = Serialize(types, Options(new TypeFactory(types)));
            cancellationToken.ThrowIfCancellationRequested();
            return new(new TypeGenerationSerializedPackage(index, serializedTypes), []);
        }
        catch (JsonException exception)
        {
            return Failure(file, exception);
        }
        catch (NotSupportedException exception)
        {
            return Failure(file, exception);
        }
        catch (ArgumentException exception)
        {
            return Failure(file, exception);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(file, exception);
        }
        catch (KeyNotFoundException exception)
        {
            return Failure(file, exception);
        }
    }

    private static JsonSerializerOptions Options(TypeFactory? factory = null)
    {
        var options = TypeSerializer.GetSerializerOptions(factory);
        options.WriteIndented = true;
        options.IndentCharacter = ' ';
        options.IndentSize = 2;
        options.NewLine = "\n";
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        var resolver = options.TypeInfoResolver
            ?? throw new InvalidOperationException("The type library did not supply serialization metadata.");
        options.TypeInfoResolver = resolver.WithAddedModifier(OmitUnusedProperties);
        return options;
    }

    private static void OmitUnusedProperties(JsonTypeInfo info)
    {
        string[] names = info.Type == typeof(TypeIndex) ? ["resourceFunctions", "namespaceFunctions"]
            : info.Type == typeof(TypeSettings) ? ["isPreview", "isDeprecated"]
            : info.Type == typeof(ResourceType) ? ["functions"]
            : [];
        foreach (var name in names)
        {
            var property = info.Properties.SingleOrDefault(property => property.Name == name)
                ?? throw new InvalidOperationException($"Missing expected serialization member '{info.Type.Name}.{name}'.");
            property.ShouldSerialize = static (_, _) => false;
        }
    }

    private static ImmutableArray<byte> Serialize<T>(T value, JsonSerializerOptions options)
    {
        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, value, options);
        stream.WriteByte((byte)'\n');
        return stream.ToArray().ToImmutableArray();
    }

    private static ImmutableArray<TypeGenDiagnostic> CheckOmittedSemantics(
        TypeGenerationLoweredPackage model,
        CancellationToken cancellationToken)
    {
        var diagnostics = ImmutableArray.CreateBuilder<TypeGenDiagnostic>();
        if (model.Index.ResourceFunctions is { Count: > 0 } || model.Index.NamespaceFunctions is { Count: > 0 })
        {
            Error("index.json contains nonempty function indexes; they cannot be silently omitted.");
        }
        if (model.Index.Settings is { IsPreview: true } or { IsDeprecated: true })
        {
            Error("index.json contains enabled legacy preview/deprecated settings; they cannot be silently omitted.");
        }
        if (model.Index.FallbackResourceType is not null)
        {
            Error("index.json contains a fallback resource, which is not supported by this writer.");
        }
        for (var index = 0; index < model.Types.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (model.Types[index] is ResourceType { Functions.Count: > 0 })
            {
                Error($"types.json#/{index} contains embedded functions; they cannot be silently omitted.");
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return diagnostics.ToImmutable();

        void Error(string message) =>
            diagnostics.Add(new(TypeGenDiagnosticCodes.BackendUnsupportedSemantic, TypeGenDiagnosticSeverity.Error, message, null));
    }

    private static TypeGenerationSerializationResult Failure(string file, Exception exception) =>
        new(null, [new(TypeGenDiagnosticCodes.BackendSerializationFailure, TypeGenDiagnosticSeverity.Error,
            $"Cannot serialize '{file}': {exception.Message}", null)]);
}
