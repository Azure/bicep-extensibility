using AutoFixture;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using System.Text;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Customizations
{
    public class EmptyKubernetesConfigCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<KubernetesConfig>(composer => composer
                .With(x => x.KubeConfig, Array.Empty<byte>())
                .With(x => x.Context, value: null));
        }
    }
}
