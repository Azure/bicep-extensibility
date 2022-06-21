namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Attributes
{
    public class AzureVoteBackServiceRequestAutoDataAttribute : SampleKubernetesRequesetAutoDataAttribute
    {
        public AzureVoteBackServiceRequestAutoDataAttribute()
            : base("core/Service@v1", @"{
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
}")
        {
        }
    }
}
