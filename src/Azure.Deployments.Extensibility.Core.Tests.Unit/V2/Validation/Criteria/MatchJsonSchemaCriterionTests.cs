// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.Tests.Unit.Fixtures.Attributes;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Validation.Criteria;
using FluentAssertions;
using Json.Pointer;
using Json.Schema;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation.Criteria
{
    public class MatchJsonSchemaCriterionTests
    {
        public class MatchJsonSchemaCriterion(JsonSchemaResolver<object?> schemaResolver, SpecVersion schemaSpecVersion = SpecVersion.Draft7)
            : MatchJsonSchemaCriterion<object?>(schemaResolver, schemaSpecVersion)
        {
        }

        private readonly static JsonPointer DummyPropertyPointer = JsonPointer.Create("root");

        private readonly static JsonNode ValidNode = new JsonObject()
        {
            ["foo"] = "bingo",
            ["bar"] = true,
        };

        private readonly static JsonObject InvalidNode = new()
        {
            ["foo"] = 123,
        };

        private readonly static JsonElement ValidElement = JsonSerializer.Deserialize<JsonElement>(ValidNode);
        private readonly static JsonElement InvalidElement = JsonSerializer.Deserialize<JsonElement>(InvalidNode);


        [Theory, AutoJsonSchema]
        public void Evaluate_ValidNode_ReturnsEmptyEnumerable(JsonSchema schema)
        {
            var sut = new MatchJsonSchemaCriterion(_ => schema);

            var errorDetails = sut.Evaluate(null, ValidNode, DummyPropertyPointer);

            errorDetails.Should().BeEmpty();
        }

        [Theory, AutoJsonSchema]
        public void Evaluate_ValidElement_ReturnsEmptyEnumerable(JsonSchema schema)
        {
            var sut = new MatchJsonSchemaCriterion(_ => schema);

            var errorDetails = sut.Evaluate(null, ValidElement, DummyPropertyPointer);

            errorDetails.Should().BeEmpty();
        }


        [Theory, AutoJsonSchema]
        public void Evaluate_InvalidNode_ReturnsErrorDetails(JsonSchema schema)
        {
            var sut = new MatchJsonSchemaCriterion(_ => schema);

            var errorDetails = sut.Evaluate(null, InvalidNode, DummyPropertyPointer);

            AssertErrorDetails(errorDetails.ToArray());
        }

        [Theory, AutoJsonSchema]
        public void Evaluate_InvalidElement_ReturnsErrorDetails(JsonSchema schema)
        {
            var sut = new MatchJsonSchemaCriterion(_ => schema);

            var errorDetails = sut.Evaluate(null, InvalidElement, DummyPropertyPointer);

            AssertErrorDetails(errorDetails.ToArray());
        }

        [Theory, AutoJsonSchema]
        public void Evaluate_PartialViolationFilter(JsonSchema schema)
        {
            var model = new FilterTestRecord(
                Properties: JsonNode.Parse(
                    """
                    {
                      "foo": "something",
                      "bar": "[reference('resource').bar]"
                    }
                    """)!,
                Unevaluated: ["/properties/bar"]);

            var filterCalls = 0;
            var propertyPointer = JsonPointer.Create("properties");

            var sut = new MatchJsonSchemaCriterion<FilterTestRecord>(_ => schema, SpecVersion.Draft7)
            {
                Filter = (violations, currentModel, modelPointer) =>
                {
                    filterCalls++;
                    currentModel.Should().BeSameAs(model);
                    modelPointer.Should().BeSameAs(propertyPointer);

                    violations.Select(violation => violation.InstanceLocation.ToString()).Should().BeEquivalentTo("/foo", "/bar");

                    return violations.Where(violation => currentModel.Unevaluated?.Contains($"{modelPointer}{violation.InstanceLocation}") is not true);
                }
            };

            var nodeErrors = sut.Evaluate(model, model.Properties, propertyPointer).ToArray();
            var elementErrors = sut.Evaluate(model, JsonSerializer.SerializeToElement(model.Properties), propertyPointer).ToArray();

            filterCalls.Should().Be(2);

            foreach (var errors in new[] { nodeErrors, elementErrors })
            {
                var error = errors.Should().ContainSingle().Subject;
                error.Code.Should().Be("JsonSchemaViolation");
                error.Target?.ToString().Should().Be("/properties/foo");
                error.Message.Should().Be(@"Value does not match the regular expression ""bingo"".");
            }
        }

        [Theory, AutoJsonSchema]
        public void Evaluate_FilterExcludesAllViolations_ReturnsNoErrors(JsonSchema schema)
        {
            var sut = new MatchJsonSchemaCriterion<object?>(_ => schema, SpecVersion.Draft7)
            {
                Filter = (violations, _, _) =>
                {
                    violations.Should().NotBeEmpty();

                    return Array.Empty<JsonSchemaViolation>();
                }
            };

            var value = JsonNode.Parse("""{ "foo": "something" }""");
            var propertyPointer = JsonPointer.Create("properties");

            var nodeErrors = sut.Evaluate(null, value, propertyPointer).ToArray();
            var elementErrors = sut.Evaluate(null, JsonSerializer.SerializeToElement(value), propertyPointer).ToArray();

            nodeErrors.Should().BeEmpty();
            elementErrors.Should().BeEmpty();
        }

        [Theory, AutoJsonSchema]
        public void Evaluate_ValidValue_DoesNotInvokeFilter(JsonSchema schema)
        {
            var sut = new MatchJsonSchemaCriterion<object?>(_ => schema, SpecVersion.Draft7)
            {
                Filter = (_, _, _) => throw new InvalidOperationException("The filter must not be invoked for valid input.")
            };

            var value = JsonNode.Parse("""{ "foo": "bingo", "bar": true }""");
            var propertyPointer = JsonPointer.Create("properties");

            var nodeErrors = sut.Evaluate(null, value, propertyPointer).ToArray();
            var elementErrors = sut.Evaluate(null, JsonSerializer.SerializeToElement(value), propertyPointer).ToArray();

            nodeErrors.Should().BeEmpty();
            elementErrors.Should().BeEmpty();
        }

        private static void AssertErrorDetails(ErrorDetail[] errorDetails)
        {
            errorDetails.Should().HaveCount(1);

            errorDetails[0].Code.Should().Be("JsonSchemaViolation");
            errorDetails[0].Message.Should().Be(@"Value is ""integer"" but should be ""string"".");
            errorDetails[0].Target?.ToString().Should().Be("/root/foo");
        }

        private record FilterTestRecord(JsonNode Properties, string[]? Unevaluated);
    }
}
