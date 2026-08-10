// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Core.Fixtures.Customizations
{
    public class GenericOperationRequestCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<ExtensibilityOperationRequest>(composer => composer
                .With(x => x.Import, ModelMapper.MapToGeneric(fixture.Create<ExtensibleImport<KubernetesConfig>>()))
                .With(x => x.Resource, ModelMapper.MapToGeneric(fixture.Create<ExtensibleResource<KubernetesResourceProperties>>())));
        }
    }
}
