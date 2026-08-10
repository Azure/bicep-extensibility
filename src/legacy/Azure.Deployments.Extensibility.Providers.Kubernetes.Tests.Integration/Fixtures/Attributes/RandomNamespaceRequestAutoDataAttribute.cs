// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Attributes
{
    public class RandomNamespaceRequestAutoDataAttribute : SampleKubernetesRequesetAutoDataAttribute
    {
        public RandomNamespaceRequestAutoDataAttribute()
            : base("core/Namespace@v1", @$"{{
  ""metadata"": {{
    ""name"": ""{new Fixture().Create<string>()}""
  }},
  ""spec"": {{}}
}}")
        {
        }
    }
}
