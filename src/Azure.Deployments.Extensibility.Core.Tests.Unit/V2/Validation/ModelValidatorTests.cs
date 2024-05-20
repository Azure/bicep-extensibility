// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;
using FluentAssertions;
using Json.Pointer;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validation
{
    public class ModelValidatorTests
    {
        public record DummyModel(int Foo, bool Bar);

        public class FooMustBeGreaterThanZero : ModelValidationRuleContext, IModelValidationRule<DummyModel>
        {
            public IEnumerable<ErrorDetail> Validate(DummyModel model)
            {
                if (model.Foo <= 0)
                {
                    yield return new("InvalidFoo", "Foo must be greater than zero.", JsonPointer.Create("/foo"));
                }
            }
        }

        public class BarMustBeTrue : ModelValidationRuleContext, IModelValidationRule<DummyModel>
        {
            public IEnumerable<ErrorDetail> Validate(DummyModel model)
            {
                if (!model.Bar)
                {
                    yield return new("InvalidBar", "Bar must be true.", JsonPointer.Create("/bar"));
                }
            }
        }

        [Fact]
        public void Validate_NoError_ReturnsNull()
        {
            var sut = new ModelValidator<DummyModel>()
                .AddRule<FooMustBeGreaterThanZero>()
                .AddRule<BarMustBeTrue>();

            var error = sut.Validate(new(100, true));

            error.Should().BeNull();
        }

        [Fact]
        public void Validate_SingleError_ReturnsErrorWithoutDetails()
        {
            var sut = new ModelValidator<DummyModel>()
                .AddRule<FooMustBeGreaterThanZero>()
                .AddRule<BarMustBeTrue>();

            var error = sut.Validate(new(100, false));
            
            error.Should().NotBeNull();
            error!.Details.Should().BeNullOrEmpty();
            error!.Code.Should().Be("InvalidBar");
            error!.Message.Should().Be("Bar must be true.");
            error!.Target.Should().BeEquivalentTo(JsonPointer.Create("/bar"));
        }

        [Fact]
        public void Validate_MultipleError_ReturnsAggregatedErrorWithDetails()
        {
            var sut = new ModelValidator<DummyModel>()
                .AddRule<FooMustBeGreaterThanZero>()
                .AddRule<BarMustBeTrue>();

            var error = sut.Validate(new(-100, false)).Should().BeOfType<Error>().Subject;

            error.Code.Should().Be("MultipleErrorsOccurred");
            error.Message.Should().Be("Multiple errors occurred. Please refer to details for more information.");
            error.Target.Should().BeNull();

            error.Details.Should().NotBeNullOrEmpty();
            error.Details!.Should().HaveCount(2);
            error.Details![0].Code.Should().Be("InvalidFoo");
            error.Details![1].Code.Should().Be("InvalidBar");
        }

        [Fact]
        public void Validate_WithBailOnErrorRule_ReturnsOnRuleValidationFailure()
        {
            var sut = new ModelValidator<DummyModel>()
                .AddRule<FooMustBeGreaterThanZero>(x => x.BailOnError = true)
                .AddRule<BarMustBeTrue>();

            var error = sut.Validate(new(-100, false));
                
            error.Should().NotBeNull();
            error!.Details.Should().BeNullOrEmpty();
            error!.Code.Should().Be("InvalidFoo");
        }
    }
}
