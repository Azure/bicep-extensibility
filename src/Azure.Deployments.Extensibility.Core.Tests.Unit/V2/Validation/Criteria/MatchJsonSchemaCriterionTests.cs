// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Tests.Unit.Fixtures.Attributes;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation.Rules;
using FluentAssertions;
using Json.Pointer;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation.Rules
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

        private static void AssertErrorDetails(ErrorDetail[] errorDetails)
        {
            errorDetails.Should().HaveCount(1);

            errorDetails[0].Code.Should().Be("JsonSchemaViolation");
            errorDetails[0].Message.Should().Be(@"Value is ""integer"" but should be ""string"".");
            errorDetails[0].Target?.ToString().Should().Be("/root/foo");
        }
    }
}
