using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using FluentAssertions;
using Xunit;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Models
{
    public class KubernetesResourcePropertiesTests
    {
        [Theory, AutoData]
        public void PatchProperty_NullAdditionalData_CreatesAdditionalData(KubernetesResourceProperties sut, string propertyName, string value)
        {
            sut.PatchProperty(propertyName, value);

            sut.AdditionalData.Should().NotBeNull();
            sut.AdditionalData![propertyName].GetString().Should().Be(value);
        }

        [Theory, AutoData]
        public void PatchProperty_NonNullAdditionalData_OverridesAdditionalData(KubernetesResourceProperties sut, string propertyName, string oldValue, string newValue)
        {
            sut.PatchProperty(propertyName, oldValue);

            sut.PatchProperty(propertyName, newValue);

            sut.AdditionalData![propertyName].GetString().Should().Be(newValue);
        }
    }
}
