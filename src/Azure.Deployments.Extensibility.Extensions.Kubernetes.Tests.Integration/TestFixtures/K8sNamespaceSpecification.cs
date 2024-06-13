// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration.TestFixtures
{
    public sealed record K8sNamespaceSpecification : K8sResourceSpecification
    {
        [SetsRequiredMembers]
        public K8sNamespaceSpecification(string namespaceName)
            : base(
                  "core/Namespace",
                  "v1",
                  $$"""
                  {
                    "metadata": {
                      "name": "{{namespaceName}}"
                    }
                  }
                  """)
        {
        }
    }
}
