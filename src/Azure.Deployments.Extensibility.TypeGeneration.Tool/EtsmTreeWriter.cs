// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal static class EtsmTreeWriter
{
    public static string Render(ExtensionTypeSemanticModel model, CancellationToken cancellationToken = default)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var ids = new Dictionary<TypeDefinition, int>(ReferenceEqualityComparer.Instance);
        writer.WriteLine($"extension {Quote(model.Name)} version={Quote(model.Version)} singleton={model.IsSingleton.ToString().ToLowerInvariant()}");
        writer.WriteLine($"  preview={model.IsPreview.ToString().ToLowerInvariant()} deprecated={model.IsDeprecated.ToString().ToLowerInvariant()}");
        if (model.FallbackResourceType is not null)
        {
            throw new ToolException("TGH1007", "Fallback resources are not supported by the Phase 1 tree writer.");
        }

        if (model.Configuration is { } configuration)
        {
            WriteType("configuration", configuration);
        }
        else
        {
            writer.WriteLine("  configuration: none");
        }

        foreach (var resource in model.ResourceTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!resource.ResourceFunctions.IsEmpty)
            {
                throw new ToolException("TGH1007", "Resource functions are not supported by the Phase 1 tree writer.");
            }

            writer.WriteLine($"  resource {Quote(resource.Name)} versions=[{string.Join(", ", resource.ApiVersions.Select(Quote))}]");
            writer.WriteLine($"    readableScopes={resource.ReadableScopes} writableScopes={resource.WritableScopes}");
            WriteType("body", resource.Body, 2);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return writer.ToString();

        void WriteType(string label, TypeDefinition root, int depth = 1)
        {
            var pending = new Stack<(string Label, TypeDefinition Type, int Depth)>();
            pending.Push((label, root, depth));
            while (pending.TryPop(out var current))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var prefix = new string(' ', current.Depth * 2) + current.Label + ": ";
                if (ids.TryGetValue(current.Type, out var existing))
                {
                    writer.WriteLine($"{prefix}ref #{existing}");
                    continue;
                }

                var id = ids.Count + 1;
                ids.Add(current.Type, id);
                var shape = current.Type switch
                {
                    NullTypeDefinition => "null",
                    BooleanTypeDefinition => "bool",
                    IntegerTypeDefinition integer => $"integer min={Number(integer.MinValue)} max={Number(integer.MaxValue)}",
                    StringTypeDefinition text => $"string sensitive={text.Sensitive.ToString().ToLowerInvariant()} minLength={Number(text.MinLength)} maxLength={Number(text.MaxLength)} pattern={Quote(text.Pattern)}",
                    StringLiteralTypeDefinition literal => $"literal {Quote(literal.Value)}",
                    ArrayTypeDefinition array => $"array minLength={Number(array.MinLength)} maxLength={Number(array.MaxLength)}",
                    ObjectTypeDefinition obj => $"object {Quote(obj.Name)} sensitive={obj.Sensitive.ToString().ToLowerInvariant()}",
                    UnionTypeDefinition => "union",
                    _ => throw new ToolException("TGH1007", $"Unsupported tree node '{current.Type.GetType().Name}'."),
                };
                writer.WriteLine($"{prefix}#{id} {shape}");
                switch (current.Type)
                {
                    case ArrayTypeDefinition array:
                        pending.Push(("item", array.ItemType, current.Depth + 1));
                        break;
                    case ObjectTypeDefinition obj:
                        if (obj.AdditionalProperties is { } additional)
                        {
                            pending.Push(("additionalProperties", additional, current.Depth + 1));
                        }

                        for (var index = obj.Properties.Length - 1; index >= 0; index--)
                        {
                            var property = obj.Properties[index];
                            pending.Push(($"property {Quote(property.Name)} flags={property.Flags} description={Quote(property.Description)}",
                                property.Type, current.Depth + 1));
                        }

                        break;
                    case UnionTypeDefinition union:
                        for (var index = union.Elements.Length - 1; index >= 0; index--)
                        {
                            pending.Push(($"member[{index}]", union.Elements[index].Type, current.Depth + 1));
                        }

                        break;
                }
            }
        }
    }

    private static string Quote(string? value) => JsonSerializer.Serialize(value);

    private static string Number(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "none";
}
