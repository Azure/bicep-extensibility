// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using FluentAssertions;
using Json.Pointer;
using Json.Schema;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Json;

public class JsonSchemaPropertyResolverTests
{
    [Fact]
    public void FindByPointer_EmptyPointer_ReturnsRootSchema()
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
    public void FindByPointer_ExistingProperty_ReturnsPropertySchema(string pointer, string propertyName)
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
    public void FindByPointer_UnresolvablePointer_ReturnsNull(string pointer)
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
    public void FindByPointer_ArrayIndex_ReturnsItemsSchema(string pointer)
    {
        var schema = JsonSchema.FromText("""{ "items": { "type": "string" } }""");

        foreach (var resolve in GetResolvers(schema, pointer))
        {
            resolve().Should().BeSameAs(schema.GetItems());
        }
    }

    [Theory]
    [InlineData("false")]
    [InlineData("""{ "type": "integer" }""")]
    public void FindByPointer_PrefixItems_IsIgnored(string items)
    {
        var schema = JsonSchema.FromText(
            $$"""
              {
                  "$schema": "http://json-schema.org/draft-07/schema#",
                  "prefixItems": [{ "type": "string" }],
                  "items": {{items}}
              }
              """);

        foreach (var resolve in GetResolvers(schema, "/0"))
        {
            resolve().Should().BeSameAs(schema.GetItems());
        }
    }

    [Theory]
    [InlineData("/0")]
    [InlineData("/1")]
    public void FindByPointer_PrefixItemsWithoutItems_ReturnsNull(string pointer)
    {
        var schema = JsonSchema.FromText("""{ "prefixItems": [{ "type": "string" }] }""");

        foreach (var resolve in GetResolvers(schema, pointer))
        {
            resolve().Should().BeNull();
        }
    }

    [Fact]
    public void FindByPointer_TupleItems_ResolvesPositionalSchemasAndNestedProperties()
    {
        var schema = JsonSchema.FromText(
            """
            {
                "$schema": "http://json-schema.org/draft-07/schema#",
                "definitions": { "value": { "type": "integer" } },
                "items": [
                    { "type": "string" },
                    { "properties": { "value": { "$ref": "#/definitions/value" } } }
                ]
            }
            """);

        foreach (var resolve in GetResolvers(schema, "/0"))
        {
            resolve().Should().BeSameAs(schema.GetItemsArrayForm()![0]);
        }

        foreach (var resolve in GetResolvers(schema, "/1/value"))
        {
            resolve().Should().BeSameAs(schema.GetDefinitions()!["value"]);
        }
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("""{ "type": "boolean" }""")]
    public void FindByPointer_TupleTrailingIndex_ReturnsAdditionalItemsSchema(string additionalItems)
    {
        var schema = JsonSchema.FromText(
            $$"""
              {
                  "items": [{ "type": "string" }],
                  "additionalItems": {{additionalItems}}
              }
              """);

        foreach (var pointer in new[] { "/1", "/42" })
        {
            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve().Should().BeSameAs(schema.GetAdditionalItems());
            }
        }
    }

    [Theory]
    [InlineData("/1")]
    [InlineData("/42")]
    public void FindByPointer_TupleWithoutAdditionalItems_TrailingIndexReturnsNull(string pointer)
    {
        var schema = JsonSchema.FromText("""{ "items": [{ "type": "string" }] }""");

        foreach (var resolve in GetResolvers(schema, pointer))
        {
            resolve().Should().BeNull();
        }
    }

    [Fact]
    public void FindByPointer_NestedArraysAndObjects_ReturnsPropertySchema()
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
    [InlineData("/00")]
    [InlineData("/01")]
    [InlineData("/2147483648")]
    [InlineData("/-")]
    [InlineData("/name")]
    [InlineData("/")]
    public void FindByPointer_InvalidArrayIndex_ReturnsNull(string pointer)
    {
        var schema = JsonSchema.FromText("""{ "items": { "type": "string" } }""");

        foreach (var resolve in GetResolvers(schema, pointer))
        {
            resolve().Should().BeNull();
        }
    }

    [Theory]
    [InlineData("0")]
    [InlineData("00")]
    [InlineData("01")]
    public void FindByPointer_NumericPropertyWithItems_PrefersPropertySchema(string propertyName)
    {
        var schema = JsonSchema.FromText(
            $$"""
              {
                  "properties": { "{{propertyName}}": { "type": "string" } },
                  "items": { "type": "integer" }
              }
              """);

        foreach (var resolve in GetResolvers(schema, $"/{propertyName}"))
        {
            resolve().Should().BeSameAs(schema.GetProperties()![propertyName]);
        }
    }

    [Theory]
    [InlineData("", "target")]
    [InlineData("/name", "name")]
    public void FindByPointer_RootReference_ResolvesBeforeTraversal(string pointer, string expectedProperty)
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

    [Fact]
    public void FindByPointer_ReferenceSiblingProperties_AreIgnored()
    {
        var schema = JsonSchema.FromText(
            """
            {
                "$schema": "http://json-schema.org/draft-07/schema#",
                "definitions": {
                    "base": { "properties": { "inherited": { "type": "integer" } } }
                },
                "$ref": "#/definitions/base",
                "properties": {
                    "name": { "type": "string" },
                    "inherited": { "type": "boolean" }
                }
            }
            """);

        foreach (var resolve in GetResolvers(schema, "/inherited"))
        {
            resolve().Should().BeSameAs(schema.GetDefinitions()!["base"].GetProperties()!["inherited"]);
        }

        foreach (var resolve in GetResolvers(schema, "/name"))
        {
            resolve().Should().BeNull();
        }
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-06/schema#")]
    [InlineData("https://json-schema.org/draft/2019-09/schema")]
    [InlineData("https://json-schema.org/draft/2020-12/schema")]
    public void Constructor_UnsupportedRootDialect_ThrowsNotSupportedException(string dialect)
    {
        var schema = JsonSchema.FromText(
            $$"""
              {
                  "$schema": "{{dialect}}",
                  "properties": { "name": { "type": "string" } }
              }
              """);

        var act = () => new JsonSchemaPropertyResolver(schema);
        act.Should().Throw<NotSupportedException>().WithMessage($"Only JSON Schema draft-07 is supported: '{dialect}'.");
    }

    [Theory]
    [InlineData("/child/name")]
    [InlineData("/alias/name")]
    public void FindByPointer_UnsupportedNestedDialect_ThrowsNotSupportedException(string pointer)
    {
        var schema = JsonSchema.FromText(
            """
            {
                "$schema": "http://json-schema.org/draft-07/schema#",
                "properties": {
                    "child": {
                        "$id": "https://example.com/child",
                        "$schema": "https://json-schema.org/draft/2020-12/schema",
                        "properties": { "name": { "type": "string" } }
                    },
                    "alias": { "$ref": "#/properties/child" }
                }
            }
            """);

        foreach (var resolve in GetResolvers(schema, pointer))
        {
            resolve.Should()
                .Throw<NotSupportedException>()
                .WithMessage("Only JSON Schema draft-07 is supported: 'https://json-schema.org/draft/2020-12/schema'.");
        }
    }

    [Theory]
    [InlineData("/alias")]
    [InlineData("/chain")]
    [InlineData("/values/0")]
    public void FindByPointer_LocalReference_ResolvesAgainstRootSchema(string pointer)
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
    public void FindByPointer_DefinitionReference_ResolvesDefinitionAndNestedProperties(string definitionsKeyword, string pointer)
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

    [Theory]
    [InlineData("#")]
    [InlineData("")]
    public void FindByPointer_NestedResource_ResolvesReferencesAgainstEnclosingResource(string recursiveReference)
    {
        var schema = JsonSchema.FromText(
            $$"""
              {
                  "$id": "https://example.com/root",
                  "definitions": { "value": { "type": "string" } },
                  "properties": {
                      "nested": {
                          "$id": "nested",
                          "definitions": { "value": { "type": "integer" } },
                          "properties": {
                              "alias": { "$ref": "#/definitions/value" },
                              "chain": { "$ref": "#/properties/alias" },
                              "child": { "$ref": "{{recursiveReference}}" }
                          }
                      },
                      "alias": { "$ref": "#/definitions/value" },
                      "nestedAlias": { "$ref": "#/properties/nested/properties/chain" }
                  }
              }
              """);

        var nested = schema.GetProperties()!["nested"];
        var expected = nested.GetDefinitions()!["value"];

        foreach (var pointer in new[] { "/nested/alias", "/nested/chain", "/nested/child/alias", "/nestedAlias" })
        {
            foreach (var resolve in GetResolvers(schema, pointer))
            {
                resolve().Should().BeSameAs(expected);
            }
        }

        foreach (var resolve in GetResolvers(schema, "/alias"))
        {
            resolve().Should().BeSameAs(schema.GetDefinitions()!["value"]);
        }
    }

    [Theory]
    [InlineData("/alias")]
    [InlineData("/nested/alias")]
    public void FindByPointer_ReferenceThroughNestedResource_UsesTargetResource(string pointer)
    {
        var schema = JsonSchema.FromText(
            """
            {
                "$defs": {
                    "nested": {
                        "$id": "https://example.com/nested",
                        "$defs": { "value": { "type": "integer" } },
                        "$ref": "#/$defs/value"
                    }
                },
                "properties": {
                    "alias": { "$ref": "#/$defs/nested" },
                    "nested": {
                        "$id": "https://example.com/other",
                        "$defs": {
                            "target": {
                                "$id": "target",
                                "$defs": { "value": { "type": "boolean" } },
                                "$ref": "#/$defs/value"
                            }
                        },
                        "properties": { "alias": { "$ref": "#/$defs/target" } }
                    }
                }
            }
            """);

        var target = pointer == "/alias"
            ? schema.GetDefs()!["nested"]
            : schema.GetProperties()!["nested"].GetDefs()!["target"];

        foreach (var resolve in GetResolvers(schema, pointer))
        {
            resolve().Should().BeSameAs(target.GetDefs()!["value"]);
        }
    }

    [Theory]
    [InlineData("#")]
    [InlineData("")]
    public void FindByPointer_RecursiveObjectReference_AllowsFiniteTraversal(string reference)
    {
        var schema = JsonSchema.FromText(
            $$"""
              {
                  "properties": {
                      "name": { "type": "string" },
                      "child": { "$ref": "{{reference}}" }
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
    [InlineData("""{ "$ref": "" }""", "")]
    [InlineData(
        """
        {
            "properties": {
                "a": { "$ref": "#/properties/b" },
                "b": { "$ref": "#/properties/a" }
            }
        }
        """, "/a")]
    public void FindByPointer_CircularReference_ThrowsInvalidOperationException(string schemaText, string pointer)
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
    public void FindByPointer_MissingReferenceTarget_ThrowsInvalidOperationException(string pointer)
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
    [InlineData("#value")]
    [InlineData("#%76alue")]
    public void FindByPointer_NamedAnchor_ThrowsNotSupportedException(string reference)
    {
        var schema = JsonSchema.FromText(
            $$"""
              {
                  "$defs": {
                      "value": { "$anchor": "value", "type": "string" }
                  },
                  "properties": {
                      "alias": { "$ref": "{{reference}}" }
                  }
              }
              """);

        foreach (var resolve in GetResolvers(schema, "/alias"))
        {
            resolve.Should()
                .Throw<NotSupportedException>()
                .WithMessage("Only local JSON Pointer schema references are supported: '#value'.");
        }
    }

    [Theory]
    [InlineData("https://example.com/schema.json", "")]
    [InlineData("other.json", "")]
    [InlineData("https://example.com/schema.json", "/alias")]
    [InlineData("other.json", "/alias")]
    public void FindByPointer_NonLocalReference_ThrowsNotSupportedException(string reference, string pointer)
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

    [Fact]
    public void FindByPointer_RepeatedReferences_ReusesResolvedReferenceChains()
    {
        SchemaKeywordRegistry.Register<CountingSchemaKeyword>();

        try
        {
            var keyword = new CountingSchemaKeyword();
            var builder = new JsonSchemaBuilder()
                .Properties(
                    ("alias", new JsonSchemaBuilder().Ref("#/counting")),
                    ("chain", new JsonSchemaBuilder().Ref("#/properties/alias")),
                    ("other", new JsonSchemaBuilder().Ref("#/properties/alias")));

            builder.Add(keyword);
            var schema = builder.Build();
            var resolver = new JsonSchemaPropertyResolver(schema);

            resolver.FindByJsonPointerSegments("chain").Should().BeSameAs(JsonSchema.True);
            var firstLookupAccessCount = keyword.SchemaAccessCount;

            resolver.FindByJsonPointerSegments("alias").Should().BeSameAs(JsonSchema.True);
            resolver.FindByJsonPointerSegments(new[] { "chain" }.AsEnumerable()).Should().BeSameAs(JsonSchema.True);
            resolver.FindByJsonPointer(JsonPointer.Parse("/other")).Should().BeSameAs(JsonSchema.True);

            keyword.SchemaAccessCount.Should().Be(firstLookupAccessCount);
        }
        finally
        {
            SchemaKeywordRegistry.Unregister<CountingSchemaKeyword>();
        }
    }

    [Fact]
    public void FindByPointer_RepeatedCalls_TraversesResourcesOnlyOnFirstReferenceLookup()
    {
        SchemaKeywordRegistry.Register<CountingSchemaKeyword>();

        try
        {
            var keyword = new CountingSchemaKeyword();
            var builder = new JsonSchemaBuilder()
                .Properties(
                    ("value", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                    ("alias", new JsonSchemaBuilder().Ref("#/properties/value")),
                    ("other", new JsonSchemaBuilder().Ref("#/properties/value")));

            builder.Add(keyword);
            var schema = builder.Build();
            var initialAccessCount = keyword.SchemaAccessCount;
            var resolver = new JsonSchemaPropertyResolver(schema);

            keyword.SchemaAccessCount.Should().Be(initialAccessCount);

            resolver.FindByJsonPointerSegments("value").Should().BeSameAs(schema.GetProperties()!["value"]);
            resolver.FindByJsonPointerSegments(new[] { "value" }.AsEnumerable()).Should().BeSameAs(schema.GetProperties()!["value"]);
            resolver.FindByJsonPointer(JsonPointer.Parse("/value")).Should().BeSameAs(schema.GetProperties()!["value"]);

            keyword.SchemaAccessCount.Should().Be(initialAccessCount);

            resolver.FindByJsonPointerSegments("alias").Should().BeSameAs(schema.GetProperties()!["value"]);
            var firstReferenceLookupAccessCount = keyword.SchemaAccessCount;
            firstReferenceLookupAccessCount.Should().BeGreaterThan(initialAccessCount);

            resolver.FindByJsonPointerSegments("alias").Should().BeSameAs(schema.GetProperties()!["value"]);
            resolver.FindByJsonPointerSegments(new[] { "other" }.AsEnumerable()).Should().BeSameAs(schema.GetProperties()!["value"]);
            resolver.FindByJsonPointer(JsonPointer.Parse("/alias")).Should().BeSameAs(schema.GetProperties()!["value"]);

            keyword.SchemaAccessCount.Should().Be(firstReferenceLookupAccessCount);
        }
        finally
        {
            SchemaKeywordRegistry.Unregister<CountingSchemaKeyword>();
        }
    }

    private static IEnumerable<Func<JsonSchema?>> GetResolvers(JsonSchema schema, string pointer)
    {
        var resolver = new JsonSchemaPropertyResolver(schema);
        var jsonPointer = JsonPointer.Parse(pointer);
        var segments = jsonPointer.ToArray();

        yield return () => resolver.FindByJsonPointer(jsonPointer);
        yield return () => resolver.FindByJsonPointerSegments(segments);
        yield return () => resolver.FindByJsonPointerSegments(segments.AsEnumerable());
    }

    [SchemaKeyword("counting")]
    private sealed class CountingSchemaKeyword : IJsonSchemaKeyword, ISchemaContainer
    {
        public int SchemaAccessCount { get; private set; }

        public JsonSchema Schema
        {
            get
            {
                this.SchemaAccessCount++;

                return JsonSchema.True;
            }
        }

        public KeywordConstraint GetConstraint(
            SchemaConstraint schemaConstraint,
            ReadOnlySpan<KeywordConstraint> localConstraints,
            EvaluationContext context)
            => KeywordConstraint.Skip;
    }
}
