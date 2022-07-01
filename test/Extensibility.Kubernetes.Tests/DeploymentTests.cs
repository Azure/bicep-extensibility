// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Extensibility.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Extensibility.Kubernetes.Tests
{
    // These tests require a connection to a Kubernetes cluster to function. 
    // You can use something like Minikube or docker Kubernetes. You should have the desired
    // cluster set as your current context ... eg: `kubectl get pod` should work against
    // the cluster you intend to use.
    [TestClass]
    public class DeploymentTests
    {
        // Put your kubeconfig here for testing, but don't check it in!
        private static readonly string Base64KubeConfig = Environment.GetEnvironmentVariable("KUBECONFIG_BASE64") ?? 
            throw new InvalidOperationException($"You must set the KUBECONFIG_BASE64 env variable before running this test. Try running:\nexport KUBECONFIG_BASE64=$(base64 -w 0 ./path/to/kubeconfig)");

        private static readonly JsonObject AzureVoteBackService = JsonObject.Parse(@"
{
  ""metadata"": {
    ""name"": ""azure-vote-back""
  },
  ""spec"": {
    ""ports"": [
      {
        ""port"": 6379
      }
    ],
    ""selector"": {
      ""app"": ""azure-vote-back""
    }
  }
}
")!.AsObject();

        private static readonly JsonObject AzureVoteBackDeployment = JsonObject.Parse(@"
{
  ""metadata"": {
    ""name"": ""azure-vote-back""
  },
  ""spec"": {
    ""replicas"": 1,
    ""selector"": {
      ""matchLabels"": {
        ""app"": ""azure-vote-back""
      }
    },
    ""template"": {
      ""metadata"": {
        ""labels"": {
          ""app"": ""azure-vote-back""
        }
      },
      ""spec"": {
        ""containers"": [
          {
            ""name"": ""azure-vote-back"",
            ""image"": ""mcr.microsoft.com/oss/bitnami/redis:6.0.8"",
            ""env"": [
              {
                ""name"": ""ALLOW_EMPTY_PASSWORD"",
                ""value"": ""yes""
              }
            ],
            ""resources"": {
              ""requests"": {
                ""cpu"": ""100m"",
                ""memory"": ""128Mi""
              },
              ""limits"": {
                ""cpu"": ""250m"",
                ""memory"": ""256Mi""
              }
            },
            ""ports"": [
              {
                ""containerPort"": 6379,
                ""name"": ""redis""
              }
            ]
          }
        ]
      }
    }
  }
}
")!.AsObject();

        [TestMethod]
        public async Task Save_AzureVoteBackService()
        {
            await CrudHelper.Save(new()
            {
                Type = "core/Service@v1",
                Import = new()
                {
                    Provider = "Kubernetes",
                    Config = new JsonObject()
                    {
                        ["kubeConfig"] = Base64KubeConfig,
                        ["namespace"] = "default",
                    },
                },
                Properties = AzureVoteBackService,
            }, CancellationToken.None);
        }

        [TestMethod]
        public async Task Save_AzureVoteBackDeployment()
        {
            await CrudHelper.Save(new()
            {
                Type = "apps/Deployment@v1",
                Import = new()
                {
                    Provider = "Kubernetes",
                    Config = new JsonObject()
                    {
                        ["kubeConfig"] = Base64KubeConfig,
                        ["namespace"] = "default",
                    },
                },
                Properties = AzureVoteBackDeployment,
            }, CancellationToken.None);
        }

        private static JsonObject GetNamespace(string name) => JsonObject.Parse($@"
{{
  ""metadata"": {{
    ""name"": ""{name}""
  }},
  ""spec"": {{}}
}}
")!.AsObject();

        [TestMethod]
        public async Task Save_and_get_Namespace()
        {
            await CrudHelper.Save(new()
            {
                Type = "core/Namespace@v1",
                Import = new()
                {
                    Provider = "Kubernetes",
                    Config = new JsonObject()
                    {
                        ["kubeConfig"] = Base64KubeConfig,
                    },
                },
                Properties = GetNamespace("save-and-get-namespace"),
            }, CancellationToken.None);

            var body = await CrudHelper.Get(new()
            {
                Type = "core/Namespace@v1",
                Import = new()
                {
                    Provider = "Kubernetes",
                    Config = new JsonObject()
                    {
                        ["kubeConfig"] = Base64KubeConfig,
                    },
                },
                Properties = GetNamespace("save-and-get-namespace"),
            }, CancellationToken.None);

            Assert.AreEqual(JsonValue.Create("test"), (dynamic)(body.Properties!)["metadata"]["name"]);
        }

              private static readonly JsonObject TestSecret = JsonObject.Parse(@"
{
  ""metadata"": {
    ""name"": ""test""
  },
  ""data"": {}
}
")!.AsObject();

        [TestMethod]
        public async Task PreviewSave_with_existing_namespace()
        {
            await CrudHelper.Save(new()
            {
                Type = "core/Namespace@v1",
                Import = new()
                {
                    Provider = "Kubernetes",
                    Config = new JsonObject()
                    {
                        ["kubeConfig"] = Base64KubeConfig,
                    },
                },
                Properties = GetNamespace("preview-save-with-existing-namespace"),
            }, CancellationToken.None);

            var body = await CrudHelper.PreviewSave(new()
            {
                Type = "core/Secret@v1",
                Import = new()
                {
                    Provider = "Kubernetes",
                    Config = new JsonObject()
                    {
                        ["kubeConfig"] = Base64KubeConfig,
                        ["namespace"] = "test",
                    },
                },
                Properties = TestSecret,
            }, CancellationToken.None);

            Assert.AreEqual(JsonValue.Create("test"), (dynamic)(body.Properties!)["metadata"]["name"]);
        }

        [TestMethod]
        public async Task PreviewSave_without_existing_namespace()
        {
            var body = await CrudHelper.PreviewSave(new()
            {
                Type = "core/Secret@v1",
                Import = new()
                {
                    Provider = "Kubernetes",
                    Config = new JsonObject()
                    {
                        ["kubeConfig"] = Base64KubeConfig,
                        ["namespace"] = "preview-save-without-existing-namespace",
                    },
                },
                Properties = TestSecret,
            }, CancellationToken.None);

            Assert.AreEqual(JsonValue.Create("test"), (dynamic)(body.Properties!)["metadata"]["name"]);
            Assert.AreEqual(JsonValue.Create("preview-save-without-existing-namespace"), (dynamic)(body.Properties!)["metadata"]["namespace"]);
        }
    }
}
