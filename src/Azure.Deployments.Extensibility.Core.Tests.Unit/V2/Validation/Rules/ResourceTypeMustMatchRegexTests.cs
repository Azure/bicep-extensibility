// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation.Rules;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Text.RegularExpressions;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation.Rules
{
    public partial class ResourceTypeMustMatchRegexTests
    {
        private readonly static ResourceTypeMustMatchRegex Sut = new() { TypePattern = ValidTypePattern() };

        [Fact]
        public void Validate_ValidResourceSpecificationType_ReturnsEmptyEnumerable()
        {
            var specification = new ResourceSpecification
            {
                Type = "foobar",
                Properties = [],
            };

            var errorDetails = Sut.Validate(specification);

            errorDetails.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ValidResourceReferenceType_ReturnsEmptyEnumerable()
        {
            var reference = new ResourceReference
            {
                Type = "foobar",
                Identifiers = [],
            };

            var errorDetails = Sut.Validate(reference);

            errorDetails.Should().BeEmpty();
        }


        [Fact]
        public void Validate_InvalidResourceSpecificationType_ReturnsSingleErrorDetail()
        {
            var specification = new ResourceSpecification
            {
                Type = "invalid",
                Properties = [],
            };

            var errorDetails = Sut.Validate(specification).ToList();

            errorDetails.Should().HaveCount(1);
            AssertInvalidTypeErrorDetail(errorDetails[0]);
        }

        [Fact]
        public void Validate_InvalidResourceReferenceType_ReturnsSingleErrorDetail()
        {
            var reference = new ResourceReference
            {
                Type = "invalid",
                Identifiers = [],
            };

            var errorDetails = Sut.Validate(reference).ToList();

            errorDetails.Should().HaveCount(1);
            AssertInvalidTypeErrorDetail(errorDetails[0]);
        }

        [GeneratedRegex("foobar")]
        private static partial Regex ValidTypePattern();

        private static void AssertInvalidTypeErrorDetail(ErrorDetail errorDetail)
        {
            using (new AssertionScope())
            {
                errorDetail.Code.Should().Be("InvalidType");
                errorDetail.Message.Should().Be("Expected the resource type 'invalid' to match the regular expression foobar.");
                errorDetail.Target?.ToString().Should().Be("/type");
            }
        }
    }
}
