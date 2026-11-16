// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using Json.Pointer;
using Json.Schema;

namespace Azure.Deployments.Extensibility.Core.V2.Json;

public static class JsonSchemaExtensions
{
    /// <summary>
    /// Tries to resolve an instance pointer relative to the supplied root schema, returning null when no property schema is found.
    /// Invalid or unsupported schema references still raise exceptions.
    /// </summary>
    public static JsonSchema? FindPropertySchemaByPointer(this JsonSchema rootSchema, JsonPointer propertyPointer)
    {
        // The pointer addresses the instance, not the schema document's /properties and /items keywords.
        var schema = ResolveReference(rootSchema, rootSchema);

        foreach (var segment in propertyPointer)
        {
            if (schema.GetProperties() is { } properties && properties.TryGetValue(segment, out var propertySchema))
            {
                schema = propertySchema;
            }
            else if (schema.GetItems() is { } items &&
                int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                schema = items;
            }
            else
            {
                return null;
            }

            schema = ResolveReference(rootSchema, schema);
        }

        return schema;
    }

    private static JsonSchema ResolveReference(JsonSchema rootSchema, JsonSchema schema)
    {
        var visited = new HashSet<JsonSchema>();

        while (schema.GetRef() is { } reference)
        {
            if (!visited.Add(schema))
            {
                throw new InvalidOperationException($"Circular schema reference '{reference}'.");
            }

            if (!reference.OriginalString.StartsWith('#'))
            {
                throw new NotSupportedException($"Only local schema references are supported: '{reference}'.");
            }

            schema = ((IBaseDocument)rootSchema).FindSubschema(JsonPointer.Parse(reference.OriginalString), new EvaluationOptions())
                ?? throw new InvalidOperationException($"Cannot resolve schema reference '{reference}'.");
        }

        return schema;
    }
}
