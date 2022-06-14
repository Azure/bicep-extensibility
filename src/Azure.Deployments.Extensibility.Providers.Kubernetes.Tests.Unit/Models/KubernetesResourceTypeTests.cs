using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using FluentAssertions;
using Xunit;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Models
{
    public class KubernetesResourceTypeTests
    {
        [Theory]
        [InlineData("apps/ControllerRevision@v1", "apps", "v1", "ControllerRevision", "apps/v1")]
        [InlineData("batch/Job@v1", "batch", "v1", "Job", "batch/v1")]
        [InlineData("foo@v100", "", "v100", "foo", "v100")]
        public void Parse_ValidRawType_ReturnsParsed(string rawType, string group, string version, string kind, string apiVersion)
        {
            var parsed = KubernetesResourceType.Parse(rawType);

            parsed.Group.Should().Be(group);
            parsed.Version.Should().Be(version);
            parsed.Kind.Should().Be(kind);
            parsed.ApiVersion.Should().Be(apiVersion);
        }

        [Theory, AutoData]
        public void Parse_InvalidRawType_ReturnsEmpty(string rawType)
        {
            var parsed = KubernetesResourceType.Parse(rawType);

            parsed.Group.Should().Be("");
            parsed.Version.Should().Be("");
            parsed.Kind.Should().Be("");
            parsed.ApiVersion.Should().Be("");
        }
    }
}
