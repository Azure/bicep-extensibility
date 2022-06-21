﻿using AutoFixture;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Core.Fixtures.Customizations;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.AutoFixture.Customizations;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.AutoFixture
{
    public class NullAdditionalDataResourcePropertiesAutoDataAttribute : AutoDataAttribute
    {
        public NullAdditionalDataResourcePropertiesAutoDataAttribute()
            : base(CreateFixture)
        {
        }

        private static IFixture CreateFixture() => new Fixture()
            .Customize(new DefaultJsonElementCustomization())
            .Customize(new NullAdditionalDataResourcePropertiesCustomization());
    }
}
