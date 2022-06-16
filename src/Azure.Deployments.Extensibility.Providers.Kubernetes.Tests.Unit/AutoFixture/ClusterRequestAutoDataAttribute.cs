using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.AutoFixture.Customizations;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.AutoFixture
{
    public class ClusterRequestAutoDataAttribute : AutoDataAttribute
    {
        public ClusterRequestAutoDataAttribute()
            : base(CreateFixture)
        {
        }

        private static IFixture CreateFixture() => new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new EmptyKubernetesConfigCustomization())
            .Customize(new NullNamespaceResourceMetadataCustomization())
            .Customize(new SampleResourceCustomization())
            .Customize(new GenericOperationRequestCustomization());
    }
}
