// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Customizations
{
    public class NullAdditionalDataResourcePropertiesCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<KubernetesResourceProperties>(composer => composer
                .With(x => x.AdditionalData, value: null));
        }
    }
}
