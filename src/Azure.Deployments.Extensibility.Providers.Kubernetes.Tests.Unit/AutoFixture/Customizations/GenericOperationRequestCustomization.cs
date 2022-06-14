using AutoFixture;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.AutoFixture.Customizations
{
    public class GenericOperationRequestCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register<JsonElement>(() => default);
            fixture.Customize<ExtensibilityOperationRequest>(composer => composer
                .With(x => x.Import, ModelMapper.MapToGeneric(fixture.Create<ExtensibleImport<KubernetesConfig>>()))
                .With(x => x.Resource, ModelMapper.MapToGeneric(fixture.Create<ExtensibleResource<KubernetesResourceProperties>>())));
        }
    }
}
