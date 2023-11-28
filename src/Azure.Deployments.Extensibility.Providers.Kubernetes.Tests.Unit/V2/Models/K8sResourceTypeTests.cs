// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;
using FluentAssertions;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.V2.Models
{
    public class K8sResourceTypeTests
    {
        [Theory, AutoData]
        public void Parse_InvalidType_Throws(string value)
        {
            var exception = Invoking(() => K8sResourceType.Parse(value)).Should().Throw<ArgumentException>().Subject;
        }

        [Theory]
        [InlineData("apps", "v1", "Deployment")]
        [InlineData("", "v1", "ServiceAccount")]
        public void Parse_ValidType_ReturnsParsedType(string group, string version, string kind)
        {
            var expected = new K8sResourceType(group, version, kind);

            var parsed = K8sResourceType.Parse(expected.ToString());

            parsed.Should().Be(expected);
        }
    }
}
