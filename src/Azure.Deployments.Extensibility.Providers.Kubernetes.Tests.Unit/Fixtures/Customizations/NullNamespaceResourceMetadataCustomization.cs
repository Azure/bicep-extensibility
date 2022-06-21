using AutoFixture;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Customizations
{
    public class NullNamespaceResourceMetadataCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<KubernetesResourceMetadata>(composer => composer
                .With(x => x.Namespace, value: null));
        }
    }
}
