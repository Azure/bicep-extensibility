// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models.Validation;
using FluentAssertions;
using Json.Schema;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Models.Validation
{
    public class JsonSchemaValidatorTests
    {
        [Fact]
        public void Validate_PatternViolation_ProducesCustomErrorMessage()
        {
            JsonSchema schema = new JsonSchemaBuilder().Properties(
                ("foobar", new JsonSchemaBuilder().Type(SchemaValueType.String).Pattern("foobar")));

            var validator = new JsonSchemaValidator(schema);

            var violations = validator.Validate(JsonNode.Parse($$"""
                {
                    "foobar": "barfoo"
                }
                """));

            var violation = violations.SingleOrDefault().Should().BeOfType<JsonSchemaViolation>().Subject;

            violation.InstanceLocation.ToString().Should().Be("/foobar");
            violation.ErrorMessage.Should().Be(@"Value does not match the regular expression ""foobar"".");
        }

        [Fact]
        public void Validate_AdditionalProperty_ProducesCustomErrorMessage()
        {
            JsonSchema schema = new JsonSchemaBuilder()
                .Properties(
                    ("foo", new JsonSchemaBuilder().Type(SchemaValueType.Boolean)))
                .AdditionalProperties(false);

            var validator = new JsonSchemaValidator(schema);

            var violations = validator.Validate(JsonNode.Parse("""
                {
                    "foo": true,
                    "bar": false
                }
                """));

            var violation = violations.SingleOrDefault().Should().BeOfType<JsonSchemaViolation>().Subject;

            violation.InstanceLocation.ToString().Should().Be($"/bar");
            violation.ErrorMessage.Should().Be(@"Value fails against the ""#/additionalProperties"": false schema.");
        }
    }
}
