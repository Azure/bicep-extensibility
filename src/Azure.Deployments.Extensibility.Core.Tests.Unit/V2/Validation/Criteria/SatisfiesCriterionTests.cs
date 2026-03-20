// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Validation.Criteria;
using FluentAssertions;
using Json.Pointer;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation.Criteria
{
    public class SatisfiesCriterionTests
    {
        public class SatisfiesCriterion(Func<string?, bool> predicate) : SatisfiesCriterion<object?, string?>(predicate)
        {
        }

        private static readonly JsonPointer DummyPointer = JsonPointer.Create("prop");

        [Fact]
        public void Evaluate_PredicateReturnsTrue_ReturnsEmptyEnumerable()
        {
            var sut = new SatisfiesCriterion(_ => true);

            var errorDetails = sut.Evaluate(null, "any", DummyPointer);

            errorDetails.Should().BeEmpty();
        }

        [Fact]
        public void Evaluate_PredicateReturnsFalse_ReturnsSingleErrorDetail()
        {
            var sut = new SatisfiesCriterion(_ => false);

            var errorDetails = sut.Evaluate(null, "any", DummyPointer).ToArray();

            errorDetails.Should().HaveCount(1);
            errorDetails[0].Code.Should().Be("ConditionNotSatisfied");
            errorDetails[0].Message.Should().Be("Value does not satisfy the required condition.");
            errorDetails[0].Target.Should().BeEquivalentTo(DummyPointer);
        }

        [Fact]
        public void Evaluate_WithErrorOverrides_UsesOverriddenCodeAndMessage()
        {
            var sut = new SatisfiesCriterion(_ => false)
            {
                ErrorCode = "CustomCode",
                ErrorMessage = "Custom message.",
            };

            var errorDetails = sut.Evaluate(null, "any", DummyPointer).ToArray();

            errorDetails.Should().HaveCount(1);
            errorDetails[0].Code.Should().Be("CustomCode");
            errorDetails[0].Message.Should().Be("Custom message.");
        }
    }
}
