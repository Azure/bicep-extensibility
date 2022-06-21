using AutoFixture;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Json;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Customizations
{
    public class SampleKubernetesResourceCustomization : ICustomization
    {
        private readonly string type;

        private readonly KubernetesResourceProperties properties;

        public SampleKubernetesResourceCustomization(string type, string properties)
        {
            this.type = type;
            this.properties =
                ExtensibilityJsonSerializer.Default.Deserialize<KubernetesResourceProperties>(properties) ??
                throw new InvalidOperationException($"Failed to deserialize properties to {nameof(KubernetesResourceProperties)}.");
        }

        public void Customize(IFixture fixture)
        {
            fixture.Customize<ExtensibleResource<KubernetesResourceProperties>>(composer => composer
                .With(x => x.Type, this.type)
                .With(x => x.Properties, this.properties));
        }
    }
}
