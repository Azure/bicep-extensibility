// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using FluentAssertions;
using Json.Pointer;
using Json.Schema;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Json
{
    public class JsonSchemaExtensionsTests
    {
        [Fact]
        public void FindPropertySchemaByPointer_EmptyPointer_ReturnsRootSchema()
        {
            var schema = JsonSchema.FromText("""{ "type": "object" }""");

            foreach (var resolve in GetResolvers(schema, ""))
            {
                resolve().Should().BeSameAs(schema);
            }
        }

        [Theory]
        [InlineData("/name", "name")]
        [InlineData("/nested/value", "value")]
        [InlineData("/a~1b", "a/b")]
        [InlineData("/a~0b", "a~b")]
        [InlineData("/", "")]
        public void FindPropertySchemaByPointer_ExistingProperty_ReturnsPropertySchema(string pointer, string propertyName)
        {
            var schema = JsonSchema.FromText(
                """
                {
                    "properties": {
                        "name": { "type": "string" },
                        "nested": {
                            "properties": {
                                "value": { "type": "integer" }
                            }
                        },
                        "a/b": { "type": "boolean" },
                        "a~b": { "type": "number" },
                        "": { "type": "null" }
                    }
                }
                """);

            var properties = pointer.StartsWith("/nested/")
                ? schema.GetProperties()!["nested"].GetProperties()!
                : schema.GetProperties()!;

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve().Should().BeSameAs(properties[propertyName]);
            }
        }

        [Theory]
        [InlineData("/missing")]
        [InlineData("/Name")]
        [InlineData("/name/child")]
        [InlineData("/nested/missing")]
        [InlineData("/properties/name")]
        public void FindPropertySchemaByPointer_UnresolvablePointer_ReturnsNull(string pointer)
        {
            var schema = JsonSchema.FromText(
                """
                {
                    "properties": {
                        "name": { "type": "string" },
                        "nested": { "type": "object" }
                    },
                    "additionalProperties": { "type": "string" }
                }
                """);

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve().Should().BeNull();
            }
        }

        [Theory]
        [InlineData("/0")]
        [InlineData("/42")]
        public void FindPropertySchemaByPointer_ArrayIndex_ReturnsItemsSchema(string pointer)
        {
            var schema = JsonSchema.FromText("""{ "items": { "type": "string" } }""");

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve().Should().BeSameAs(schema.GetItems());
            }
        }

        [Fact]
        public void FindPropertySchemaByPointer_NestedArraysAndObjects_ReturnsPropertySchema()
        {
            var schema = JsonSchema.FromText(
                """
                {
                    "properties": {
                        "matrix": {
                            "items": {
                                "items": {
                                    "properties": {
                                        "value": { "type": "integer" }
                                    }
                                }
                            }
                        }
                    }
                }
                """);

            var expected = schema.GetProperties()!["matrix"].GetItems()!.GetItems()!.GetProperties()!["value"];

            foreach (var resolve in GetResolvers(schema, "/matrix/0/1/value"))
            {
                resolve().Should().BeSameAs(expected);
            }
        }

        [Theory]
        [InlineData("/-1")]
        [InlineData("/+1")]
        [InlineData("/1.0")]
        [InlineData("/ 1")]
        [InlineData("/1 ")]
        [InlineData("/2147483648")]
        [InlineData("/-")]
        [InlineData("/name")]
        [InlineData("/")]
        public void FindPropertySchemaByPointer_InvalidArrayIndex_ReturnsNull(string pointer)
        {
            var schema = JsonSchema.FromText("""{ "items": { "type": "string" } }""");

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve().Should().BeNull();
            }
        }

        [Fact]
        public void FindPropertySchemaByPointer_NumericPropertyWithItems_PrefersPropertySchema()
        {
            var schema = JsonSchema.FromText(
                """
                {
                    "properties": { "0": { "type": "string" } },
                    "items": { "type": "integer" }
                }
                """);

            foreach (var resolve in GetResolvers(schema, "/0"))
            {
                resolve().Should().BeSameAs(schema.GetProperties()!["0"]);
            }
        }

        [Theory]
        [InlineData("", "target")]
        [InlineData("/name", "name")]
        public void FindPropertySchemaByPointer_RootReference_ResolvesBeforeTraversal(string pointer, string expectedProperty)
        {
            var schema = JsonSchema.FromText(
                """
                {
                    "$ref": "#/properties/target",
                    "properties": {
                        "target": {
                            "properties": {
                                "name": { "type": "string" }
                            }
                        }
                    }
                }
                """);

            var target = schema.GetProperties()!["target"];
            var expected = expectedProperty == "target" ? target : target.GetProperties()![expectedProperty];

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve().Should().BeSameAs(expected);
            }
        }

        [Theory]
        [InlineData("/alias")]
        [InlineData("/chain")]
        [InlineData("/values/0")]
        public void FindPropertySchemaByPointer_LocalReference_ResolvesAgainstRootSchema(string pointer)
        {
            var schema = JsonSchema.FromText(
                """
                {
                    "properties": {
                        "target": { "type": "string" },
                        "alias": { "$ref": "#/properties/target" },
                        "chain": { "$ref": "#/properties/alias" },
                        "values": { "items": { "$ref": "#/properties/chain" } }
                    }
                }
                """);

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve().Should().BeSameAs(schema.GetProperties()!["target"]);
            }
        }

        [Theory]
        [InlineData("definitions", "/address")]
        [InlineData("definitions", "/address/street")]
        [InlineData("$defs", "/address")]
        [InlineData("$defs", "/address/street")]
        public void FindPropertySchemaByPointer_DefinitionReference_ResolvesDefinitionAndNestedProperties(string definitionsKeyword, string pointer)
        {
            var schema = JsonSchema.FromText(
                $$"""
                  {
                      "{{definitionsKeyword}}": {
                          "address": {
                              "type": "object",
                              "properties": {
                                  "street": { "type": "string" }
                              }
                          }
                      },
                      "properties": {
                          "address": { "$ref": "#/{{definitionsKeyword}}/address" }
                      }
                  }
                  """);

            var definitions = definitionsKeyword == "$defs" ? schema.GetDefs()! : schema.GetDefinitions()!;
            var address = definitions["address"];
            var expected = pointer == "/address" ? address : address.GetProperties()!["street"];

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve().Should().BeSameAs(expected);
            }
        }

        [Fact]
        public void FindPropertySchemaByPointer_RecursiveObjectReference_AllowsFiniteTraversal()
        {
            var schema = JsonSchema.FromText(
                """
                {
                    "properties": {
                        "name": { "type": "string" },
                        "child": { "$ref": "#" }
                    }
                }
                """);

            foreach (var resolve in GetResolvers(schema, "/child/child/name"))
            {
                resolve().Should().BeSameAs(schema.GetProperties()!["name"]);
            }
        }

        [Theory]
        [InlineData("""{ "$ref": "#" }""", "")]
        [InlineData(
            """
            {
                "properties": {
                    "a": { "$ref": "#/properties/b" },
                    "b": { "$ref": "#/properties/a" }
                }
            }
            """, "/a")]
        public void FindPropertySchemaByPointer_CircularReference_ThrowsInvalidOperationException(string schemaText, string pointer)
        {
            var schema = JsonSchema.FromText(schemaText);

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("Circular schema reference '*'.");
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("/alias")]
        public void FindPropertySchemaByPointer_MissingReferenceTarget_ThrowsInvalidOperationException(string pointer)
        {
            var schema = JsonSchema.FromText(
                pointer == ""
                    ? """{ "$ref": "#/properties/missing" }"""
                    : """{ "properties": { "alias": { "$ref": "#/properties/missing" } } }""");

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("Cannot resolve schema reference '#/properties/missing'.");
            }
        }

        [Theory]
        [InlineData("https://example.com/schema.json", "")]
        [InlineData("other.json", "")]
        [InlineData("https://example.com/schema.json", "/alias")]
        [InlineData("other.json", "/alias")]
        public void FindPropertySchemaByPointer_NonLocalReference_ThrowsNotSupportedException(string reference, string pointer)
        {
            var schema = JsonSchema.FromText(
                pointer == ""
                    ? $$"""{ "$ref": "{{reference}}" }"""
                    : $$"""{ "properties": { "alias": { "$ref": "{{reference}}" } } }""");

            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve.Should()
                    .Throw<NotSupportedException>()
                    .WithMessage($"Only local schema references are supported: '{reference}'.");
            }
        }

        private static IEnumerable<Func<JsonSchema?>> GetResolvers(JsonSchema schema, string pointer)
        {
            var jsonPointer = JsonPointer.Parse(pointer);
            var segments = jsonPointer.ToArray();

            yield return () => schema.FindPropertySchemaByJsonPointer(jsonPointer);
            yield return () => schema.FindPropertySchemaByJsonPointerSegments(segments);
            yield return () => schema.FindPropertySchemaByJsonPointerSegments(segments.AsEnumerable());
        }
    }
}
