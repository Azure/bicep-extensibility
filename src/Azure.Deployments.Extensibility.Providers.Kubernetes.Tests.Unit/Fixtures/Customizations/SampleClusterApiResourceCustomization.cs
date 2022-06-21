using AutoFixture;
using k8s.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Customizations
{
    public class SampleClusterApiResourceCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<V1APIResource>(composer => composer
                .With(x => x.Namespaced, false)
                .With(x => x.Name, "samplePlural")
                .With(x => x.Kind, "sampleKind"));
        }
    }
}
