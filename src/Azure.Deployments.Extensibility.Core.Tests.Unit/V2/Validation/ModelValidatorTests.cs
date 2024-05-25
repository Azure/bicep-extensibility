// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Validation;
using FluentAssertions;
using Json.Pointer;
using System.Text.RegularExpressions;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation
{
    public partial class ModelValidatorTests
    {
        public record DummyModel(int? Foo, bool? Bar, string? Baz);

        public partial class DummyModelValidator : ModelValidator<DummyModel>
        {
            public DummyModelValidator()
            {
                this.AnyValid(x => x.Foo)
                    .MustNotBeNull().WithErrorCode("FooMustNotBeNull");

                this.AnyValid(x => x.Bar)
                    .MustNotBeNull().WithErrorCode("BarMustNotBeNull");

                this.WhenPrecedingRulesSatisfied(x => x.Baz)
                    .MustNotBeNull()
                        .WithErrorCode("BazMustNotBeNull").AndThen
                    .MustMatchRegex(DummyRegex())
                        .WithErrorCode("BazMustMatchRegex")
                        .WithErrorMessage("Baz must match regex.");
            }

            [GeneratedRegex("bicep")]
            private static partial Regex DummyRegex();
        }

        private readonly static DummyModelValidator Sut = new();

        [Fact]
        public void Validate_NoError_ReturnsNull()
        {
            var error = Sut.Validate(new DummyModel(123, true, "bicep"));

            error.Should().BeNull();
        }

        [Fact]
        public void Validate_SingleError_ReturnsErrorWithoutDetails()
        {
            var error = Sut.Validate(new(null, true, "foobar"));

            error.Should().NotBeNull();
            error!.Details.Should().BeNullOrEmpty();
            error!.Code.Should().Be("FooMustNotBeNull");
            error!.Target.Should().BeEquivalentTo(JsonPointer.Create("foo"));
        }

        [Fact]
        public void Validate_MultipleErrors_ReturnsAggregatedErrorWithDetails()
        {
            var error = Sut.Validate(new(null, null, "foobar"));

            error.Should().NotBeNull();
            error!.Code.Should().Be("MultipleErrorsOccurred");
            error!.Message.Should().Be("Multiple errors occurred. Please refer to details for more information.");
            error!.Target.Should().BeNull();

            error.Details.Should().NotBeNullOrEmpty();
            error.Details!.Should().HaveCount(2);
            error.Details![0].Code.Should().Be("FooMustNotBeNull");
            error.Details![1].Code.Should().Be("BarMustNotBeNull");
        }

        [Fact]
        public void Validate_SingleDepdencyRuleFailed_SkipsDepdentRule()
        {
            var error = Sut.Validate(new(null, true, "xyz"));

            error.Should().NotBeNull();
            error!.Details.Should().BeNullOrEmpty();
            error!.Code.Should().Be("FooMustNotBeNull");
            error!.Target.Should().BeEquivalentTo(JsonPointer.Create("foo"));
        }

        [Fact]
        public void Validate_MultipleDepdencyRuleFailed_SkipsDepdentRule()
        {
            var error = Sut.Validate(new(null, null, "xyz"));

            error.Should().NotBeNull();
            error!.Code.Should().Be("MultipleErrorsOccurred");
            error!.Message.Should().Be("Multiple errors occurred. Please refer to details for more information.");
            error!.Target.Should().BeNull();

            error.Details.Should().NotBeNullOrEmpty();
            error.Details!.Should().HaveCount(2);
            error.Details![0].Code.Should().Be("FooMustNotBeNull");
            error.Details![1].Code.Should().Be("BarMustNotBeNull");
        }

        [Fact]
        public void Validate_DepdencyRulesPassed_ValidatesDepdenentRule()
        {
            var error = Sut.Validate(new(123, true, "xyz"));

            error.Should().NotBeNull();
            error!.Details.Should().BeNullOrEmpty();
            error!.Code.Should().Be("BazMustMatchRegex");
            error!.Message.Should().Be("Baz must match regex.");
            error!.Target.Should().BeEquivalentTo(JsonPointer.Create("baz"));
        }
    }
}
