using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Attributes;
using FluentAssertions;
using Xunit;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Models
{
    public class KubernetesResourcePropertiesTests
    {
        [Theory, NullAdditionalDataResourcePropertiesAutoData]
        public void PatchProperty_NullAdditionalData_CreatesAdditionalData(KubernetesResourceProperties sut, string propertyName, string value)
        {
            var patched = sut.PatchProperty(propertyName, value);

            patched.AdditionalData.Should().NotBeNull();
            patched.AdditionalData![propertyName].GetString().Should().Be(value);
        }

        [Theory, NullAdditionalDataResourcePropertiesAutoData]
        public void PatchProperty_ExistingAdditionalData_OverridesAdditionalData(KubernetesResourceProperties sut, string propertyName, string oldValue, string newValue)
        {
            var patched = sut
                .PatchProperty(propertyName, oldValue)
                .PatchProperty(propertyName, newValue);

            patched.AdditionalData![propertyName].GetString().Should().Be(newValue);
        }
    }
}
