// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Core.Fixtures.Customizations;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Customizations
{
    public class SampleKubernetesRequestCustomization : CompositeCustomization
    {
        public SampleKubernetesRequestCustomization(string @namespace, string resourceType, string resourceProperties)
            : base(
                  new DefaultJsonElementCustomization(),
                  new FileSystemKubernetesConfigCustomization(@namespace),
                  new SampleKubernetesResourceCustomization(resourceType, resourceProperties),
                  new GenericOperationRequestCustomization())
        {
        }
    }
}
