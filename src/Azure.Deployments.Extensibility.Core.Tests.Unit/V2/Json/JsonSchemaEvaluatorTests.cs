// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Tests.Unit.Fixtures.Attributes;
using Azure.Deployments.Extensibility.Core.V2.Json;
using FluentAssertions;
using Json.Schema;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Json
{
    public class JsonSchemaEvaluatorTests
    {
        [Theory, AutoJsonSchema]
        public void Evaluate_PatternViolation_ProducesCustomErrorMessage(JsonSchema schema)
        {
            var sut = new JsonSchemaEvaluator(schema);

            var violations = sut.Evaluate(JsonNode.Parse("""
                {
                    "foo": "something",
                    "bar": false
                }
                """)).ToList();

            violations.Should().HaveCount(1);
            violations[0].InstanceLocation.ToString().Should().Be("/foo");
            violations[0].ErrorMessage.Should().Be(@"Value does not match the regular expression ""bingo"".");
        }

        [Theory, AutoJsonSchema]
        public void Evaluate_AdditionalProperty_ProducesCustomErrorMessage(JsonSchema schema)
        {
            var sut = new JsonSchemaEvaluator(schema);

            var violations = sut.Evaluate(JsonNode.Parse("""
                {
                    "foobar": 123
                }
                """));

            var violation = violations.SingleOrDefault().Should().BeOfType<JsonSchemaViolation>().Subject;

            violation.InstanceLocation.ToString().Should().Be($"/foobar");
            violation.ErrorMessage.Should().Be(@"Value fails against the ""#/additionalProperties"": false schema.");
        }
    }
}
