// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Tests.Unit.Fixtures.Attributes;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation.Rules;
using FluentAssertions;
using Json.Schema;
using System.Text.Json.Nodes;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation.Rules
{
    public class ResourcePropertiesMustMatchSchemaTests
    {
        private readonly static JsonObject ValidProperties = new()
        {
            ["foo"] = "bingo",
            ["bar"] = true,
        };

        private readonly static JsonObject InvalidProperties = new()
        {
            ["foo"] = 123,
        };

        [Fact]
        public void Validate_SchemaAndSchemaResolverNotSpecified_Throws()
        {
            var sut = new ResourcePropertiesMustMatchSchema();
            var specification = CreateResourceSpecification(ValidProperties);

            Invoking(() => sut.Validate(specification).ToList())
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Either Schema or SchemaResolver must be provided and cannot be null.");
        }

        [Theory, AutoJsonSchema]
        public void Validate_ValidProperties_ReturnsEmptyEnumerable(JsonSchema schema)
        {
            var sut = new ResourcePropertiesMustMatchSchema() { Schema = schema };
            var specification = CreateResourceSpecification(ValidProperties);

            var errorDetails = sut.Validate(specification);

            errorDetails.Should().BeEmpty();
        }

        [Theory, AutoJsonSchema]
        public void Validate_InvalidProperties_ReturnsErrorDetails(JsonSchema schema)
        {
            var sut = new ResourcePropertiesMustMatchSchema() { Schema = schema };
            var specification = CreateResourceSpecification(InvalidProperties);

            var errorDetails = sut.Validate(specification);

            AssertInvalidPropertiesErrorDetails(errorDetails.ToList());
        }

        private static ResourceSpecification CreateResourceSpecification(JsonObject properties) => new()
        {
            Type = "foobar",
            Properties = properties,
            Config = [],
        };

        private static void AssertInvalidPropertiesErrorDetails(List<ErrorDetail> errorDetails)
        {
            errorDetails.Should().HaveCount(1);

            errorDetails[0].Code.Should().Be("InvalidProperty");
            errorDetails[0].Message.Should().Be(@"Value is ""integer"" but should be ""string"".");
            errorDetails[0].Target?.ToString().Should().Be("/properties/foo");
        }

    }
}
