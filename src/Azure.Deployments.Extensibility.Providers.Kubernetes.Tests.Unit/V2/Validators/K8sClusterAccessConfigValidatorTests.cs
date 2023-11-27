// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Validators;
using FluentAssertions;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.V2.Validators
{
    public class K8sClusterAccessConfigValidatorTests
    {
        [Theory, AutoData]
        public void Validate_KubeConfigIsNotBase64Encoded_ReturnsSingleElementList(K8sClusterAccessConfigValidator sut, string kubeConfig)
        {
            var configObject = new JsonObject
            {
                ["kubeConfig"] = kubeConfig,
            };

            var errorDetails = sut.Validate(configObject);

            errorDetails.Should().HaveCount(1);
            errorDetails[0].Code.Should().Be("InvalidKubeConfig");
            errorDetails[0].Message.Should().Be("Value must be a Base64-encoded string.");
            errorDetails[0].Target.Should().NotBeNull();
            errorDetails[0].Target!.ToString().Should().Be("/config/kubeConfig");
        }

        [Theory, AutoData]
        public void Validate_KubeConfigIsBase64Encoded_ReturnsEmptyList(K8sClusterAccessConfigValidator sut, string kubeConfig)
        {
            var configObject = new JsonObject
            {
                ["kubeConfig"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(kubeConfig)),
            };

            var errorDetails = sut.Validate(configObject);

            errorDetails.Should().BeEmpty();
        }
    }
}
