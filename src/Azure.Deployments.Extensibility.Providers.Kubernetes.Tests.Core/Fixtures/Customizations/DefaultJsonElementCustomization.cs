// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Core.Fixtures.Customizations
{
    public class DefaultJsonElementCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register<JsonElement>(() => default);
        }
    }
}
