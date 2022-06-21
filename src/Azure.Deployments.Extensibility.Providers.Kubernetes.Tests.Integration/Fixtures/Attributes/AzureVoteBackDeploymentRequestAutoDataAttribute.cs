namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Attributes
{
    public class AzureVoteBackDeploymentRequestAutoDataAttribute : SampleKubernetesRequesetAutoDataAttribute
    {
        public AzureVoteBackDeploymentRequestAutoDataAttribute()
            : base("apps/Deployment@v1", @"{
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
}")
        {
        }
    }
}
