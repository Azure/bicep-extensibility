// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Core.Fixtures.Customizations;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Customizations;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Attributes
{
    public class NamespacedRequestAutoDataAttribute : AutoDataAttribute
    {
        public NamespacedRequestAutoDataAttribute()
            : base(CreateFixture)
        {
        }

        private static IFixture CreateFixture() => new Fixture()
            .Customize(new DefaultJsonElementCustomization())
            .Customize(new EmptyKubernetesConfigCustomization())
            .Customize(new NullAdditionalDataResourcePropertiesCustomization())
            .Customize(new SampleResourceCustomization())
            .Customize(new GenericOperationRequestCustomization());
    }
}
