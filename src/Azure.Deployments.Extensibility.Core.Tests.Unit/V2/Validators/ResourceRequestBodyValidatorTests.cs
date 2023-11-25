// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.Tests.Unit.Fixtures.Attributes;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validators;
using FluentAssertions;
using Json.Pointer;
using Moq;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Validators
{
    public class ResourceRequestBodyValidatorTests
    {
        [Theory, AutoMoqData]
        public void Validate_SingleErrorDetials_ConvertsAndReturnsError(
            [Frozen] Mock<IResourceTypeValidator> resourceTypeValidatorMock,
            [Frozen] Mock<IResourcePropertiesValidator> resourcePropertiesValidatorMock,
            [Frozen] Mock<IResourceConfigValidator> resourceConfigValidatorMock,
            string resourceType,
            ResourceRequestBodyValidator sut)
        {
            resourceTypeValidatorMock.Setup(x => x.Validate(It.IsAny<string>()))
                .Returns(new ErrorDetail[]
                {
                    new("TypeError", "Type error.", JsonPointer.Parse("/type")),
                });

            resourcePropertiesValidatorMock.Setup(x => x.Validate(It.IsAny<JsonObject>()))
                .Returns(Array.Empty<ErrorDetail>());

            resourceConfigValidatorMock.Setup(x => x.Validate(It.IsAny<JsonObject?>()))
                .Returns(Array.Empty<ErrorDetail>());

            var dummyResourceRequestBoby = new ResourceRequestBody { Type = resourceType, Properties = new JsonObject() };

            var result = sut.Validate(dummyResourceRequestBoby);

            var error = result.Should().BeOfType<Error>().Subject;
            error.Code.Should().Be("TypeError");
            error.Message.Should().Be("Type error.");
            error.Target?.ToString().Should().Be("/type");
            error.Details.Should().BeNull();
        }

        [Theory, AutoMoqData]
        public void Validate_MultipleErrorDetails_ReturnsAggregatedError(
            [Frozen] Mock<IResourceTypeValidator> resourceTypeValidatorMock,
            [Frozen] Mock<IResourcePropertiesValidator> resourcePropertiesValidatorMock,
            [Frozen] Mock<IResourceConfigValidator> resourceConfigValidatorMock,
            string resourceType,
            ResourceRequestBodyValidator sut)
        {
            resourceTypeValidatorMock.Setup(x => x.Validate(It.IsAny<string>()))
                .Returns(new ErrorDetail[]
                {
                    new("TypeError", "Type error.", JsonPointer.Parse("/type")),
                });

            resourcePropertiesValidatorMock.Setup(x => x.Validate(It.IsAny<JsonObject>()))
                .Returns(new ErrorDetail[]
                {
                    new("PropError", "Property error one.", JsonPointer.Parse("/properties/propOne")),
                    new("PropError", "Property error two.", JsonPointer.Parse("/properties/propTwo"))
                });

            resourceConfigValidatorMock.Setup(x => x.Validate(It.IsAny<JsonObject?>()))
                .Returns(new ErrorDetail[]
                {
                    new("ConfigError", "Config error one.", JsonPointer.Parse("/config/configOne")),
                    new("ConfigError", "Config error two.", JsonPointer.Parse("/config/configTwo"))
                });

            var dummyResourceRequestBoby = new ResourceRequestBody { Type = resourceType, Properties = new JsonObject() };

            var result = sut.Validate(dummyResourceRequestBoby);

            var error = result.Should().BeOfType<Error>().Subject;
            error.Target.Should().BeNull();
            error.Message.Should().Be("Multiple error occurred. Please see details for more information.");
            error.Details.Should().HaveCount(5);

            var errorDetails = error.Details!;

            var typeError = errorDetails.Single(x => x.Message.Equals("Type error."));
            typeError.Code.Should().Be("TypeError");
            typeError.Target?.ToString().Should().Be("/type");

            var propErrorOne = errorDetails.Single(x => x.Message.Equals("Property error one."));
            propErrorOne.Code.Should().Be("PropError");
            propErrorOne.Target?.ToString().Should().Be("/properties/propOne");

            var propErrorTwo = errorDetails.Single(x => x.Message.Equals("Property error two."));
            propErrorTwo.Code.Should().Be("PropError");
            propErrorTwo.Target?.ToString().Should().Be("/properties/propTwo");

            var configErrorOne = errorDetails.Single(x => x.Message.Equals("Config error one."));
            configErrorOne.Code.Should().Be("ConfigError");
            configErrorOne.Target?.ToString().Should().Be("/config/configOne");

            var configErrorTwo = errorDetails.Single(x => x.Message.Equals("Config error two."));
            configErrorTwo.Code.Should().Be("ConfigError");
            configErrorTwo.Target?.ToString().Should().Be("/config/configTwo");
        }
    }
}
