﻿using AutoFixture;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.AutoFixture.Customizations
{
    public class SampleResourceCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<ExtensibleResource<KubernetesResourceProperties>>(composer => composer
                .With(x => x.Type, "sampleGroup/sampleKind@v1"));
        }
    }
}
