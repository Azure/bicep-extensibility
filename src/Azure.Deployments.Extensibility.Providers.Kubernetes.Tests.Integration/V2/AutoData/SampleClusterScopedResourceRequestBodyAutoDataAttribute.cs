// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.V2.AutoData
{
    public class SampleClusterScopedResourceRequestBodyAutoDataAttribute(string? @namespace = null) : KubernetesResourceRequestBodyAutoDataAttribute(
        "Service@v1",
        """
        {
          "metadata": {
            "name": "azure-vote-back"
          },
          "spec": {
            "ports": [
              {
                "port": 6379
              }
            ],
            "selector": {
              "app": "azure-vote-back"
            }
          }
        }
        """,
        @namespace)
    {
    }
}
