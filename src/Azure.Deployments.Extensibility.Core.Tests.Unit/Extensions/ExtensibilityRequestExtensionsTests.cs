using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Azure.Deployments.Extensibility.Core.Extensions;
using FluentAssertions;
using Json.Schema;
using System.Text.RegularExpressions;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.Extensions
{
    public readonly record struct InvalidConfig(int Width, bool Height, int Length);

    public readonly record struct ValidConfig(int Width, int Height);

    public readonly record struct InvalidProperties(string Foo, string Bar);

    public readonly record struct ValidProperties(string Bar);

    public class ExtensibilityRequestExtensionsTests
    {
        private readonly static JsonSchema SampleImportConfigSchema = new JsonSchemaBuilder()
            .Properties(
                ("width", schema: new JsonSchemaBuilder().Type(SchemaValueType.Integer)),
                ("height", new JsonSchemaBuilder().Type(SchemaValueType.Integer)))
            .Required("width", "height")
            .AdditionalProperties(false);

        private static readonly Regex SampleResourceTypeRegex = new("TYPE", RegexOptions.Compiled);

        private readonly static JsonSchema SampleResourcePropertiesSchema = new JsonSchemaBuilder()
            .Properties(
                ("foo", new JsonSchemaBuilder().Type(SchemaValueType.String).Pattern("FOO")),
                ("bar", new JsonSchemaBuilder().Type(SchemaValueType.String)))
            .Required("bar");

        [Theory, AutoData]
        public void Validate_ValidRequest_MapsModels(ExtensibleImport<ValidConfig> import, ExtensibleResource<ValidProperties> resource)
        {
            resource = resource with { Type = "TYPE" };

            var sut = new ExtensibilityOperationRequest(ModelMapper.MapToGeneric(import), ModelMapper.MapToGeneric(resource));

            var (mappedImport, mappedResource) = sut.Validate<ValidConfig, ValidProperties>(SampleImportConfigSchema, SampleResourceTypeRegex, SampleResourcePropertiesSchema);

            mappedImport.Should().Be(import);
            mappedResource.Should().Be(resource);
        }

        [Theory, AutoData]
        public void Validate_InvalidRequest_ThrowsException(ExtensibleImport<InvalidConfig> import, ExtensibleResource<InvalidProperties> resource)
        {
            resource = resource with { Type = "TYPE" };

            var sut = new ExtensibilityOperationRequest(ModelMapper.MapToGeneric(import), ModelMapper.MapToGeneric(resource));

            var errors = Invoking(() => sut.Validate<InvalidConfig, InvalidProperties>(SampleImportConfigSchema, SampleResourceTypeRegex, SampleResourcePropertiesSchema))
                .Should()
                .Throw<ExtensibilityException>()
                .Which.Errors.ToArray();

            errors.Should().HaveCount(3);

            errors[0].Target.Should().Be(import.GetJsonPointer(x => x.Config.Height));
            errors[0].Message.Should().Be(@"Value is ""false"" but should be ""integer"".");

            errors[1].Target.Should().Be(import.GetJsonPointer(x => x.Config.Length));
            errors[1].Message.Should().Be(@"Value fails against the ""#/additionalProperties"": false schema.");

            errors[2].Target.Should().Be(resource.GetJsonPointer(x => x.Properties).Combine("foo"));
            errors[2].Message.Should().Be(@"Value does not match the regular expression ""FOO"".");
        }

        [Theory, AutoData]
        public void Validate_InvalidResourceType_SkipsValidatingResourceProperties(ExtensibleImport<ValidConfig> import, ExtensibleResource<InvalidProperties> resource)
        {
            var sut = new ExtensibilityOperationRequest(ModelMapper.MapToGeneric(import), ModelMapper.MapToGeneric(resource));

            var errors = Invoking(() => sut.Validate<InvalidConfig, InvalidProperties>(SampleImportConfigSchema, SampleResourceTypeRegex, SampleResourcePropertiesSchema))
                .Should()
                .Throw<ExtensibilityException>()
                .Which.Errors.ToArray();

            errors.Should().HaveCount(1);

            errors[0].Target.Should().Be(resource.GetJsonPointer(x => x.Type));
            errors[0].Message.Should().Be(@"Value does not match the regular expression ""TYPE"".");
        }
    }
}
