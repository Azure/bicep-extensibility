// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using AutoFixture.Dsl;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Fixtures.Customizations
{
    public class ResourceMetadataCustomization : ICustomization
    {
        private readonly bool withNamespace;

        public ResourceMetadataCustomization(bool withNamespace)
        {
            this.withNamespace = withNamespace;
        }

        public void Customize(IFixture fixture)
        {
            fixture.Customize<KubernetesResourceMetadata>(composer =>
            {
                var updatedComposer = composer.With(x => x.AdditionalData, value: null);

                if (!this.withNamespace)
                {
                    updatedComposer = updatedComposer.With(x => x.Namespace, value: null);
                }

                return updatedComposer;
            });
        }
    }
}
