// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.V2.Validation.Rules;
using FluentAssertions;
using Json.Pointer;
using System.Text.RegularExpressions;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation.Criteria
{
    public partial class MatchRegexCriterionTests
    {
        public class MatchRegexCriterion(Regex regex) : MatchRegexCriterion<object?>(regex)
        {
        }

        [Fact]
        public void Evaluate_ValidValue_ReturnsEmptyEnumerable()
        {
            var sut = new MatchRegexCriterion(DummyRegex());

            var errorDetails = sut.Evaluate(null, "foobar", JsonPointer.Empty);

            errorDetails.Should().BeEmpty();
        }


        [Theory, AutoData]
        public void Evaluate_InvalidValue_ReturnsSingleErrorDetail(string invalidValue)
        {
            var sut = new MatchRegexCriterion(DummyRegex());

            var errorDetails = sut.Evaluate(null, invalidValue, JsonPointer.Empty).ToArray();

            errorDetails.Should().HaveCount(1);
            errorDetails[0].Code.Should().Be("RegularExpressionMismatch");
            errorDetails[0].Message.Should().Be("Value does not match the regular expression /^foobar$/.");
            errorDetails[0].Target.Should().BeEquivalentTo(JsonPointer.Empty);
        }

        [GeneratedRegex("^foobar$")]
        private static partial Regex DummyRegex();
    }
}
