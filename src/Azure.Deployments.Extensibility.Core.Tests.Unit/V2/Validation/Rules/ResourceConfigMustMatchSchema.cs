// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.Tests.Unit.Extensions;
using Azure.Deployments.Extensibility.Core.Tests.Unit.Fixtures.Attributes;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation.Rules;
using FluentAssertions;
using Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation.Rules
{
    public class ResourceConfigMustMatchSchemaTests
    {
        private readonly static JsonObject ValidConfig = new()
        {
            ["foo"] = "bingo",
            ["bar"] = true,
        };

        private readonly static JsonObject InvalidConfig = new()
        {
            ["foo"] = 123,
        };

        [Fact]
        public void Validate_SchemaNotSpecified_Throws()
        {
            var sut = new ResourceConfigMustMatchSchema();
            var specification = CreateResourceSpecification(ValidConfig);

            Invoking(() => sut.Validate(specification).ToList())
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Schema must be provided and cannot be null.");
        }

        [Theory, AutoJsonSchema]
        public void Validate_ValidResourceSpecificationConfig_ReturnsEmptyEnumerable(JsonSchema schema)
        {
            var sut = new ResourceConfigMustMatchSchema() { Schema = schema };
            var specification = CreateResourceSpecification(ValidConfig);

            var errorDetails = sut.Validate(specification);

            errorDetails.Should().BeEmpty();
        }

        [Theory, AutoJsonSchema]
        public void Validate_ValidResourceReferenceConfig_ReturnsEmptyEnumerable(JsonSchema schema)
        {
            var sut = new ResourceConfigMustMatchSchema() { Schema = schema };
            var reference = CreateResourceReference(ValidConfig);

            var errorDetails = sut.Validate(reference);

            errorDetails.Should().BeEmpty();
        }

        [Theory, AutoJsonSchema]
        public void Validate_InvalidResourceSpecificationConfig_ReturnsErrorDetails(JsonSchema schema)
        {
            var sut = new ResourceConfigMustMatchSchema() { Schema = schema };
            var specification = CreateResourceSpecification(InvalidConfig);

            var errorDetails = sut.Validate(specification);

            AssertInvalidConfigErrorDetails(errorDetails.ToList());
        }

        [Theory, AutoJsonSchema]
        public void Validate_InvalidResourceReferenceConfig_ReturnsErrorDetails(JsonSchema schema)
        {
            var sut = new ResourceConfigMustMatchSchema() { Schema = schema };
            var reference = CreateResourceReference(InvalidConfig);

            var errorDetails = sut.Validate(reference);

            AssertInvalidConfigErrorDetails(errorDetails.ToList());
        }

        private static ResourceSpecification CreateResourceSpecification(JsonObject config) => new()
        {
            Type = "foobar",
            Properties = [],
            Config = config,
        };

        private static ResourceReference CreateResourceReference(JsonObject config) => new()
        {
            Type = "foobar",
            Identifiers = [],
            Config = config,
        };

        private static void AssertInvalidConfigErrorDetails(List<ErrorDetail> errorDetails)
        {
            errorDetails.Should().HaveCount(1);

            errorDetails[0].Code.Should().Be("InvalidConfig");
            errorDetails[0].Message.Should().Be(@"Value is ""integer"" but should be ""string"".");
            errorDetails[0].Target?.ToString().Should().Be("/config/foo");
        }
    }
}
