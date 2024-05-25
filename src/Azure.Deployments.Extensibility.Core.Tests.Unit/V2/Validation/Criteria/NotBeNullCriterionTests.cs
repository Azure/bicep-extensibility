// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Validation.Criteria;
using FluentAssertions;
using Json.Pointer;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation.Criteria
{
    public class NotBeNullCriterionTests
    {
        public class NotBeNullCriterion : NotBeNullCriterion<object?, object?>
        {
        }

        [Fact]
        public void Evaluate_NonNullValue_ReturnsEmptyEnumerable()
        {
            var sut = new NotBeNullCriterion();

            var errorDetails = sut.Evaluate(null, 123, JsonPointer.Empty);

            errorDetails.Should().BeEmpty();
        }


        [Fact]
        public void Evaluate_NullValue_ReturnsSingleErrorDetail()
        {
            var sut = new NotBeNullCriterion();

            var errorDetails = sut.Evaluate(null, null, JsonPointer.Empty).ToArray();

            errorDetails.Should().HaveCount(1);
            errorDetails[0].Code.Should().Be("ValueMustNotBeNull");
            errorDetails[0].Message.Should().Be("Value must not be null.");
            errorDetails[0].Target.Should().BeEquivalentTo(JsonPointer.Empty);
        }
    }
}
