using AutoFixture.Xunit2;
using Azure.ResourceManager.Extensibility.Core.Validators;
using FluentAssertions;
using FluentValidation;
using Json.Schema;
using System.Text.Json;
using Xunit;

namespace Azure.ResourceManager.Extensibility.Core.Tests.Unit.Validators
{
    public class JsonSchemaValidatorTests
    {
        [Theory, AutoData]
        public void Validate_InvalidJsonElement_ReturnsErrors(InvalidProperties properties, TestDataValidator sut)
        {
            var result = sut.Validate(new TestData(properties));

            result.IsValid.Should().BeFalse();

            result.Errors.Count.Should().Be(3);
            result.Errors.Should().OnlyContain(x => x.ErrorCode.Equals("JsonSchemaViolation"));

            result.Errors[0].PropertyName.Should().Be("/properties/propA");
            result.Errors[0].ErrorMessage.Should().Be(@"Value does not match the pattern of ""TEST"".");

            result.Errors[1].PropertyName.Should().Be("/properties");
            result.Errors[1].ErrorMessage.Should().Be(@"Required properties [""propB""] were not present.");

            result.Errors[2].PropertyName.Should().Be("/properties/propC");
            result.Errors[2].ErrorMessage.Should().Be("The property is not allowed.");
        }

        [Theory, AutoData]
        public void Validate_ValidJsonElement_ReturnsNoError(ValidProperties properties, TestDataValidator sut)
        {
            var result = sut.Validate(new TestData(properties));

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        public record TestData
        {
            private static readonly JsonSerializerOptions SerializerOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            public TestData(InvalidProperties properties)
            {
                this.Properties = JsonSerializer.SerializeToElement(properties, SerializerOptions);
            }

            public TestData(ValidProperties properties)
            {
                this.Properties = JsonSerializer.SerializeToElement(properties, SerializerOptions);
            }

            public JsonElement Properties { get; }
        };

        public readonly record struct InvalidProperties(string PropA, bool PropC);

        public readonly record struct ValidProperties(int PropB)
        {
            public string PropA { get; } = "TEST";
        }

        public class TestDataValidator : AbstractValidator<TestData>
        {
            private static readonly JsonSchema TestSchema = new JsonSchemaBuilder()
                .Properties(
                    ("propA", new JsonSchemaBuilder().Type(SchemaValueType.String).Pattern("TEST")),
                    ("propB", new JsonSchemaBuilder().Type(SchemaValueType.Integer)))
                .Required("propA", "propB")
                .AdditionalProperties(false);

            public TestDataValidator()
            {
                this.RuleFor(x => x.Properties).MustConformTo(TestSchema);
            }
        }
    }
}
