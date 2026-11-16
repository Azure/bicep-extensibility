// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Globalization;
using Json.Pointer;
using Json.Schema;

namespace Azure.Deployments.Extensibility.Core.V2.Json;

/// <summary>
/// Resolves instance pointers to property schemas in a JSON schema. Only dialect draft-07 is supported. Schemas without a declared dialect
/// are treated as draft-07. References must resolve within the supplied document; external schemas are not loaded.
/// </summary>
public class JsonSchemaPropertyResolver
{
    private readonly JsonSchema rootSchema;
    private readonly Lazy<SchemaIndex> schemaIndex;
    private readonly ConcurrentDictionary<JsonSchema, JsonSchema> resolvedReferences = new(ReferenceEqualityComparer.Instance);

    public JsonSchemaPropertyResolver(JsonSchema rootSchema)
    {
        ValidateDialect(rootSchema);

        this.rootSchema = rootSchema;
        this.schemaIndex = new(() => BuildSchemaIndex(rootSchema));
    }

    /// <summary>
    /// Tries to resolve an instance pointer relative to the supplied root schema, returning null when no property schema is found.
    /// Invalid schema references will raise exceptions.
    /// </summary>
    public JsonSchema? FindByJsonPointerSegments(IEnumerable<string> pointerSegments)
    {
        // The pointer addresses the instance, not the schema document's /properties and /items keywords.
        var schema = this.ResolveReference(this.rootSchema);

        foreach (var segment in pointerSegments)
        {
            if (schema.GetProperties() is { } properties && properties.TryGetValue(segment, out var propertySchema))
            {
                schema = propertySchema;
            }
            else if ((segment.Length == 1 || !segment.StartsWith('0')) &&
                int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                schema = schema.GetItemsArrayForm() is { } tupleItems
                    ? index < tupleItems.Count ? tupleItems[index] : schema.GetAdditionalItems()
                    : schema.GetItems();

                if (schema is null)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            schema = this.ResolveReference(schema);
        }

        return schema;
    }

    /// <summary>
    /// Tries to resolve an instance pointer relative to the supplied root schema, returning null when no property schema is found.
    /// Invalid schema references will raise exceptions.
    /// </summary>
    public JsonSchema? FindByJsonPointerSegments(params string[] pointerSegments)
        => this.FindByJsonPointerSegments((IEnumerable<string>)pointerSegments);

    /// <summary>
    /// Tries to resolve an instance pointer relative to the supplied root schema, returning null when no property schema is found.
    /// Invalid schema references will raise exceptions.
    /// </summary>
    public JsonSchema? FindByJsonPointer(JsonPointer propertyPointer)
        => this.FindByJsonPointerSegments(propertyPointer);

    private static void ValidateDialect(JsonSchema schema)
    {
        if (schema.GetSchema() is { } dialect && dialect != MetaSchemas.Draft7Id)
        {
            throw new NotSupportedException($"Only JSON Schema draft-07 is supported: '{dialect}'.");
        }
    }

    private static SchemaIndex BuildSchemaIndex(JsonSchema rootSchema)
    {
        var index = new SchemaIndex();
        var documentUri = new Uri(rootSchema.BaseUri.GetLeftPart(UriPartial.Query));
        index.Identifiers.Add(SchemaIdentifier.FromUri(documentUri), rootSchema);
        var pending = new Stack<(JsonSchema Schema, Uri BaseUri)>();
        pending.Push((rootSchema, documentUri));

        while (pending.TryPop(out var entry))
        {
            var (schema, baseUri) = entry;

            if (schema.GetRef() is null && schema.GetId() is { } id)
            {
                baseUri = new Uri(baseUri, id);
                var identifier = SchemaIdentifier.FromUri(baseUri);

                if (!index.Identifiers.TryAdd(identifier, schema) &&
                    !ReferenceEquals(index.Identifiers[identifier], schema))
                {
                    throw new InvalidOperationException($"Duplicate schema identifier '{baseUri}'.");
                }
            }

            if (!index.BaseUris.TryAdd(schema, baseUri))
            {
                continue;
            }

            if (schema.GetRef() is null)
            {
                ValidateDialect(schema);
            }

            foreach (var keyword in schema.Keywords ?? [])
            {
                if (keyword is ISchemaContainer { Schema: { } child })
                {
                    pending.Push((child, baseUri));
                }

                if (keyword is ISchemaCollector { Schemas: { } children })
                {
                    foreach (var subschema in children)
                    {
                        pending.Push((subschema, baseUri));
                    }
                }

                if (keyword is IKeyedSchemaCollector { Schemas: { } namedChildren })
                {
                    foreach (var subschema in namedChildren.Values)
                    {
                        pending.Push((subschema, baseUri));
                    }
                }

                if (keyword is ICustomSchemaCollector { Schemas: { } customChildren })
                {
                    foreach (var subschema in customChildren)
                    {
                        pending.Push((subschema, baseUri));
                    }
                }
            }
        }

        return index;
    }

    private JsonSchema ResolveReference(JsonSchema schema)
    {
        HashSet<JsonSchema>? visited = null;

        while (true)
        {
            if (this.resolvedReferences.TryGetValue(schema, out var resolved))
            {
                schema = resolved;

                break;
            }

            if (schema.GetRef() is not { } reference)
            {
                ValidateDialect(schema);
                this.resolvedReferences.TryAdd(schema, schema);

                break;
            }

            visited ??= new HashSet<JsonSchema>(ReferenceEqualityComparer.Instance);

            if (!visited.Add(schema))
            {
                throw new InvalidOperationException($"Circular schema reference '{reference}'.");
            }

            var index = this.schemaIndex.Value;
            var baseUri = index.BaseUris[schema];
            // An empty reference drops a fragment inherited from a named $id.
            var referenceUri = reference.OriginalString.Length == 0
                ? new Uri(baseUri.GetLeftPart(UriPartial.Query))
                : new Uri(baseUri, reference);

            var identifier = SchemaIdentifier.FromUri(referenceUri);
            JsonSchema? target;

            if (identifier.Fragment.StartsWith('/'))
            {
                target = index.Identifiers.TryGetValue(identifier with { Fragment = string.Empty }, out var resourceRoot)
                    ? ((IBaseDocument)resourceRoot).FindSubschema(JsonPointer.Parse(identifier.Fragment), new EvaluationOptions())
                    : null;
            }
            else
            {
                index.Identifiers.TryGetValue(identifier, out target);
            }

            schema = target
                ?? throw new InvalidOperationException($"Cannot resolve schema reference '{reference}'.");
        }

        if (visited is not null)
        {
            foreach (var referenceSchema in visited)
            {
                this.resolvedReferences.TryAdd(referenceSchema, schema);
            }
        }

        return schema;
    }

    private sealed class SchemaIndex
    {
        public Dictionary<JsonSchema, Uri> BaseUris { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<SchemaIdentifier, JsonSchema> Identifiers { get; } = [];
    }

    private sealed record SchemaIdentifier(Uri DocumentUri, string Fragment)
    {
        public static SchemaIdentifier FromUri(Uri uri)
            => new(
                new Uri(uri.GetLeftPart(UriPartial.Query)),
                uri.Fragment.Length == 0 ? string.Empty : Uri.UnescapeDataString(uri.Fragment[1..]));
    }
}
