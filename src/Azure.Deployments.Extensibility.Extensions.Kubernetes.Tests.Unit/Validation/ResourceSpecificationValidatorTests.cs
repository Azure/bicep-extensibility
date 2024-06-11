// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation;
using Azure.Deployments.Extensibility.TestFixtures;
using FluentAssertions;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Unit.Validation
{
    public class ResourceSpecificationValidatorTests
    {
        [Theory, AutoMoqData]
        internal void Validate_InvalidTypeNullApiVersionNullConfig_ReturnsError(Fixture fixture, ResourceSpecificationValidator sut)
        {
            var invalidResourceSpecification = fixture.Build<ResourceSpecification>()
                .With(x => x.ApiVersion, (string?)null)
                .With(x => x.Properties, [])
                .With(x => x.Config, (JsonObject?)null)
                .Create();

            var error = sut.Validate(invalidResourceSpecification)!;

            error.Should().NotBeNull();
            error.Code.Should().Be("MultipleErrorsOccurred");
            error.Details.Should().NotBeNull();
            error.Details.Should().HaveCount(3);

            var errorDetails = error.Details!;

            errorDetails[0].Code.Should().Be("InvalidResourceType");
            errorDetails[1].Code.Should().Be("NullApiVersion");
            errorDetails[2].Code.Should().Be("NullConfig");
        }

        [Theory, AutoMoqData]
        internal void Validate_InvalidApiVersionInvalidConfig_ReturnsError(Fixture fixture, ResourceSpecificationValidator sut)
        {
            var invalidResourceReference = fixture.Build<ResourceSpecification>()
                .With(x => x.Type, "apps/Deployment")
                .With(x => x.ApiVersion, "@$#")
                .With(x => x.Properties, [])
                .With(x => x.Config, [])
                .Create();

            var error = sut.Validate(invalidResourceReference)!;

            error.Should().NotBeNull();
            error.Code.Should().Be("MultipleErrorsOccurred");
            error.Details.Should().NotBeNull();
            error.Details.Should().HaveCount(2);

            var errorDetails = error.Details!;

            errorDetails[0].Code.Should().Be("InvalidApiVersion");
            errorDetails[1].Code.Should().Be("InvalidConfig");
            errorDetails[1].Message.Should().Be(@"Required properties [""kubeConfig""] are not present.");
        }

        [Theory, AutoMoqData]
        internal void Validate_InvalidPropertiesNullConfig_ReturnsError(Fixture fixture, ResourceSpecificationValidator sut)
        {
            var invalidResourceReference = fixture.Build<ResourceSpecification>()
                .With(x => x.Type, "apps/Deployment")
                .With(x => x.ApiVersion, "v1")
                .With(x => x.Properties, new JsonObject()
                {
                    ["metadata"] = new JsonObject()
                    {
                        ["name"] = fixture.Create<int>(),
                    },
                })
                .With(x => x.Config, (JsonObject?)null)
                .Create();

            var error = sut.Validate(invalidResourceReference)!;

            error.Should().NotBeNull();
            error.Code.Should().Be("MultipleErrorsOccurred");
            error.Details.Should().NotBeNull();
            error.Details.Should().HaveCount(2);

            var errorDetails = error.Details!;

            errorDetails[0].Code.Should().Be("InvalidProperty");
            errorDetails[0].Message.Should().Be(@"Value is ""integer"" but should be ""string"".");
            errorDetails[0].Target.Should().BeEquivalentTo(JsonPointer.Create("properties", "metadata", "name"));
            errorDetails[1].Code.Should().Be("NullConfig");
        }

        [Theory, AutoMoqData]
        internal void Validate_ValidReference_ReturnsNull(Fixture fixture, ResourceSpecificationValidator sut)
        {
            var invalidResourceReference = fixture.Build<ResourceSpecification>()
                .With(x => x.Type, "apps/Deployment")
                .With(x => x.ApiVersion, "v1")
                .With(x => x.Properties, new JsonObject()
                {
                    ["metadata"] = new JsonObject()
                    {
                        ["name"] = fixture.Create<string>(),
                        ["namespace"] = fixture.Create<string>(),
                    },
                })
                .With(x => x.Config, new JsonObject()
                {
                    ["kubeConfig"] = "kubeConfig",
                })
                .Create();

            var error = sut.Validate(invalidResourceReference)!;

            error.Should().BeNull();
        }

    }
}
