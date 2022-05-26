using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using k8s.Models;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions
{
    public static class KubernetesResourcePropertiesExtensions
    {
        public static V1Patch ToV1Patch(this KubernetesResourceProperties properties)
        {
            var propertiesContent = JsonSerializer.Serialize(properties, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            });


            return new(propertiesContent, V1Patch.PatchType.ApplyPatch);
        }
    }
}
