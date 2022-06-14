using AutoFixture;
using k8s.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.AutoFixture.Customizations
{
    public class SampleNamespacedApiResourceCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<V1APIResource>(composer => composer
                .With(x => x.Namespaced, true)
                .With(x => x.Name, "samplePlural")
                .With(x => x.Kind, "sampleKind"));
        }
    }
}
