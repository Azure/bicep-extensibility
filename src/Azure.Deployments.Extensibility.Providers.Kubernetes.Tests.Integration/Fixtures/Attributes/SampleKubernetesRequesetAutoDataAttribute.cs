using AutoFixture;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Customizations;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Attributes
{
    public abstract class SampleKubernetesRequesetAutoDataAttribute : AutoDataAttribute
    {
        public SampleKubernetesRequesetAutoDataAttribute(string resourceType, string resourceProperties)
            : this("default", resourceType, resourceProperties)
        {
        }

        public SampleKubernetesRequesetAutoDataAttribute(string @namespace, string resourceType, string resourceProperties)
            : base(() => CreateFixture(@namespace, resourceType, resourceProperties))
        {
        }

        private static IFixture CreateFixture(string @namespace, string resourceType, string resourceProperties) => new Fixture()
            .Customize(new SampleKubernetesRequestCustomization(@namespace, resourceType, resourceProperties));
    }
}
