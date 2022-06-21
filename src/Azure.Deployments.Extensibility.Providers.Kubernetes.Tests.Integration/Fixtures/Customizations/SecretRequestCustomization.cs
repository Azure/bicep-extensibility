namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Customizations
{
    public class SecretRequestCustomization : SampleKubernetesRequestCustomization
    {
        public SecretRequestCustomization(string @namespace)
            : base(@namespace, "core/Secret@v1", @"{
  ""metadata"": {
    ""name"": ""test-secret""
  },
  ""data"": {}
}")
        {
        }
    }
}
