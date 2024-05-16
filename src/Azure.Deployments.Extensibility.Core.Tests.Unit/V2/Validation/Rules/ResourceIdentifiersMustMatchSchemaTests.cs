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
    public class ResourceIdentifiersMustMatchSchemaTests
    {
        private readonly static JsonObject ValidIdentifiers = new()
        {
            ["foo"] = "bingo",
            ["bar"] = true,
        };

        private readonly static JsonObject InvalidIdentifiers = new()
        {
            ["foo"] = 123,
        };

        [Fact]
        public void Validate_SchemaAndSchemaResolverNotSpecified_Throws()
        {
            var sut = new ResourceIdentifiersMustMatchSchema();
            var reference = CreateResourceReference(ValidIdentifiers);

            Invoking(() => sut.Validate(reference).ToList())
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Either Schema or SchemaResolver must be provided and cannot be null.");
        }

        [Theory, AutoJsonSchema]
        public void Validate_ValidIdentifiers_ReturnsEmptyEnumerable(JsonSchema schema)
        {
            var sut = new ResourceIdentifiersMustMatchSchema() { Schema = schema };
            var reference = CreateResourceReference(ValidIdentifiers);

            var errorDetails = sut.Validate(reference);

            errorDetails.Should().BeEmpty();
        }

        [Theory, AutoJsonSchema]
        public void Validate_InvalidIdentifiers_ReturnsErrorDetails(JsonSchema schema)
        {
            var sut = new ResourceIdentifiersMustMatchSchema() { Schema = schema };
            var reference = CreateResourceReference(InvalidIdentifiers);

            var errorDetails = sut.Validate(reference);

            AssertInvalidIdentifiersErrorDetails(errorDetails.ToList());
        }

        private static ResourceReference CreateResourceReference(JsonObject identifiers) => new()
        {
            Type = "foobar",
            Identifiers = identifiers,
            Config = [],
        };

        private static void AssertInvalidIdentifiersErrorDetails(List<ErrorDetail> errorDetails)
        {
            errorDetails.Should().HaveCount(1);

            errorDetails[0].Code.Should().Be("InvalidIdentifier");
            errorDetails[0].Message.Should().Be(@"Value is ""integer"" but should be ""string"".");
            errorDetails[0].Target?.ToString().Should().Be("/identifiers/foo");
        }
    }
}
