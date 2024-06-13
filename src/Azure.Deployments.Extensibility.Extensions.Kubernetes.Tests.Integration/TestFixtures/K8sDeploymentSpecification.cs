// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration.TestFixtures
{
    public sealed record K8sDeploymentSpecification : K8sResourceSpecification
    {
        [SetsRequiredMembers]
        public K8sDeploymentSpecification(string name, string? @namespace)
            : this(name)
        {
            if (@namespace is not null)
            {
                this.Properties.SetPropertyValue("/metadata/namespace", @namespace);
            }
        }

        [SetsRequiredMembers]
        private K8sDeploymentSpecification(string name)
            : base(
                  "apps/Deployment",
                  "v1",
                  $$"""
                  {
                    "metadata": {
                      "name": "{{name}}"
                    },
                    "spec": {
                      "replicas": 1,
                      "selector": {
                        "matchLabels": {
                          "app": "azure-vote-back"
                        }
                      },
                      "template": {
                        "metadata": {
                          "labels": {
                            "app": "azure-vote-back"
                          }
                        },
                        "spec": {
                          "containers": [
                            {
                              "name": "azure-vote-back",
                              "image": "mcr.microsoft.com/oss/bitnami/redis:6.0.8",
                              "env": [
                                {
                                  "name": "ALLOW_EMPTY_PASSWORD",
                                  "value": "yes"
                                }
                              ],
                              "resources": {
                                "requests": {
                                  "cpu": "100m",
                                  "memory": "128Mi"
                                },
                                "limits": {
                                  "cpu": "250m",
                                  "memory": "256Mi"
                                }
                              },
                              "ports": [
                                {
                                  "containerPort": 6379,
                                  "name": "redis"
                                }
                              ]
                            }
                          ]
                        }
                      }
                    }
                  }
                  """)
        {
        }

        public override string ToString() => base.ToString();
    }
}
