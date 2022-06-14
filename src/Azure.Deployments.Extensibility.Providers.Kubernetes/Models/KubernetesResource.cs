using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions;
using k8s;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Models
{
    public class KubernetesResource
    {
        public KubernetesResource(IKubernetes kubernetes, string group, string version, string? @namespace, string plural, ExtensibleResource<KubernetesResourceProperties> resource)
        {
            this.Kubernetes = kubernetes;
            this.Group = group;
            this.Version = version;
            this.Namespace = @namespace;
            this.Plural = plural;
            this.Resource = resource;
        }

        public IKubernetes Kubernetes { get; }

        public string Group { get; }

        public string Version { get; }

        public string? Namespace { get; }

        public string Plural { get; }

        public ExtensibleResource<KubernetesResourceProperties> Resource { get; }

        public async Task<JsonElement> GetAsync(CancellationToken cancellationToken)
        {
            var result = this.Namespace is not null
                ? await this.Kubernetes.CustomObjects.GetNamespacedCustomObjectAsync(
                    this.Group,
                    this.Version,
                    this.Namespace,
                    this.Plural,
                    this.Resource.Properties.Metadata.Name,
                    cancellationToken)
                : await this.Kubernetes.CustomObjects.GetClusterCustomObjectAsync(
                    this.Group,
                    this.Version,
                    this.Plural,
                    this.Resource.Properties.Metadata.Name,
                    cancellationToken);

            return JsonSerializer.SerializeToElement(result);
        }

        public async Task<JsonElement> PatchAsync(string? dryRun, CancellationToken cancellationToken)
        {
            var result = this.Namespace is not null
                ? await this.Kubernetes.CustomObjects.PatchNamespacedCustomObjectAsync(
                    this.Resource.Properties.ToV1Patch(),
                    this.Group,
                    this.Version,
                    this.Namespace,
                    this.Plural,
                    this.Resource.Properties.Metadata.Name,
                    fieldManager: "Bicep",
                    force: true,
                    dryRun: dryRun,
                    cancellationToken: cancellationToken)
                : await this.Kubernetes.CustomObjects.PatchClusterCustomObjectAsync(
                    this.Resource.Properties.ToV1Patch(),
                    this.Group,
                    this.Version,
                    this.Plural,
                    this.Resource.Properties.Metadata.Name,
                    fieldManager: "Bicep",
                    force: true,
                    dryRun: dryRun,
                    cancellationToken: cancellationToken);

            return JsonSerializer.SerializeToElement(result);
        }

        public async Task<JsonElement> DeleteAsync(CancellationToken cancellationToken)
        {
            var result = this.Namespace is not null
                ? await this.Kubernetes.CustomObjects.DeleteNamespacedCustomObjectAsync(
                    this.Group,
                    this.Version,
                    this.Namespace,
                    this.Plural,
                    this.Resource.Properties.Metadata.Name,
                    cancellationToken: cancellationToken)
                : await this.Kubernetes.CustomObjects.DeleteClusterCustomObjectAsync(
                    this.Group,
                    this.Version,
                    this.Plural,
                    this.Resource.Properties.Metadata.Name,
                    cancellationToken: cancellationToken);

            return JsonSerializer.SerializeToElement(result);
        }
    }
}
