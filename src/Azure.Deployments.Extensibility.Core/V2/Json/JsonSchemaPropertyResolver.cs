// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using Json.Pointer;
using Json.Schema;

namespace Azure.Deployments.Extensibility.Core.V2.Json;

public class JsonSchemaPropertyResolver
{
    private readonly JsonSchema rootSchema;
    private readonly Lazy<Dictionary<JsonSchema, JsonSchema>> resourceRoots;
    private readonly Dictionary<JsonSchema, JsonSchema> resolvedReferences = new(ReferenceEqualityComparer.Instance);

    public JsonSchemaPropertyResolver(JsonSchema rootSchema)
    {
        this.rootSchema = rootSchema;
        this.resourceRoots = new(() => GetResourceRoots(rootSchema));
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
                schema = schema.GetPrefixItems() is { } prefixItems && index < prefixItems.Count
                    ? prefixItems[index]
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

    private static Dictionary<JsonSchema, JsonSchema> GetResourceRoots(JsonSchema rootSchema)
    {
        var resourceRoots = new Dictionary<JsonSchema, JsonSchema>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(JsonSchema Schema, JsonSchema ResourceRoot)>();
        pending.Push((rootSchema, rootSchema));

        while (pending.TryPop(out var entry))
        {
            var (schema, resourceRoot) = entry;

            if (schema.GetId() is not null)
            {
                resourceRoot = schema;
            }

            if (!resourceRoots.TryAdd(schema, resourceRoot))
            {
                continue;
            }

            foreach (var keyword in schema.Keywords ?? [])
            {
                if (keyword is ISchemaContainer { Schema: { } child })
                {
                    pending.Push((child, resourceRoot));
                }

                if (keyword is ISchemaCollector { Schemas: { } children })
                {
                    foreach (var subschema in children)
                    {
                        pending.Push((subschema, resourceRoot));
                    }
                }

                if (keyword is IKeyedSchemaCollector { Schemas: { } namedChildren })
                {
                    foreach (var subschema in namedChildren.Values)
                    {
                        pending.Push((subschema, resourceRoot));
                    }
                }

                if (keyword is ICustomSchemaCollector { Schemas: { } customChildren })
                {
                    foreach (var subschema in customChildren)
                    {
                        pending.Push((subschema, resourceRoot));
                    }
                }
            }
        }

        return resourceRoots;
    }

    private JsonSchema ResolveReference(JsonSchema schema)
    {
        if (this.resolvedReferences.TryGetValue(schema, out var resolved))
        {
            return resolved;
        }

        HashSet<JsonSchema>? visited = null;

        while (schema.GetRef() is { } reference)
        {
            if (this.resolvedReferences.TryGetValue(schema, out resolved))
            {
                schema = resolved;

                break;
            }

            visited ??= new HashSet<JsonSchema>(ReferenceEqualityComparer.Instance);

            if (!visited.Add(schema))
            {
                throw new InvalidOperationException($"Circular schema reference '{reference}'.");
            }

            if (!reference.OriginalString.StartsWith('#'))
            {
                throw new NotSupportedException($"Only local schema references are supported: '{reference}'.");
            }

            schema = ((IBaseDocument)this.resourceRoots.Value[schema]).FindSubschema(JsonPointer.Parse(reference.OriginalString), new EvaluationOptions())
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
}
