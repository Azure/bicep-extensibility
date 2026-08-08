// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Json;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions;
using k8s;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Models
{
    public class KubernetesResource : IDisposable
    {
        private volatile bool disposed;

        public KubernetesResource(IKubernetes kubernetes, string group, string version, string? @namespace, string plural, KubernetesResourceProperties properties)
        {
            this.Kubernetes = kubernetes;
            this.Group = group;
            this.Version = version;
            this.Namespace = @namespace;
            this.Plural = plural;
            this.Properties = properties;
        }

        public IKubernetes Kubernetes { get; }

        public string Group { get; }

        public string Version { get; }

        public string? Namespace { get; }

        public string Plural { get; }

        public KubernetesResourceProperties Properties { get; }

        public async Task<JsonElement> GetAsync(CancellationToken cancellationToken)
        {
            var result = this.Namespace is not null
                ? await this.Kubernetes.CustomObjects.GetNamespacedCustomObjectAsync(
                    this.Group,
                    this.Version,
                    this.Namespace,
                    this.Plural,
                    this.Properties.Metadata.Name,
                    cancellationToken)
                : await this.Kubernetes.CustomObjects.GetClusterCustomObjectAsync(
                    this.Group,
                    this.Version,
                    this.Plural,
                    this.Properties.Metadata.Name,
                    cancellationToken);

            return ExtensibilityJsonSerializer.Default.SerializeToElement(result);
        }

        public async Task<JsonElement> PatchAsync(string? dryRun, CancellationToken cancellationToken)
        {
            var result = this.Namespace is not null
                ? await this.Kubernetes.CustomObjects.PatchNamespacedCustomObjectAsync(
                    this.Properties.ToV1Patch(),
                    this.Group,
                    this.Version,
                    this.Namespace,
                    this.Plural,
                    this.Properties.Metadata.Name,
                    fieldManager: "Bicep",
                    force: true,
                    dryRun: dryRun,
                    cancellationToken: cancellationToken)
                : await this.Kubernetes.CustomObjects.PatchClusterCustomObjectAsync(
                    this.Properties.ToV1Patch(),
                    this.Group,
                    this.Version,
                    this.Plural,
                    this.Properties.Metadata.Name,
                    fieldManager: "Bicep",
                    force: true,
                    dryRun: dryRun,
                    cancellationToken: cancellationToken);

            return ExtensibilityJsonSerializer.Default.SerializeToElement(result);
        }

        public async Task<JsonElement> DeleteAsync(CancellationToken cancellationToken)
        {
            var result = this.Namespace is not null
                ? await this.Kubernetes.CustomObjects.DeleteNamespacedCustomObjectAsync(
                    this.Group,
                    this.Version,
                    this.Namespace,
                    this.Plural,
                    this.Properties.Metadata.Name,
                    cancellationToken: cancellationToken)
                : await this.Kubernetes.CustomObjects.DeleteClusterCustomObjectAsync(
                    this.Group,
                    this.Version,
                    this.Plural,
                    this.Properties.Metadata.Name,
                    cancellationToken: cancellationToken);

            return ExtensibilityJsonSerializer.Default.SerializeToElement(result);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing && !this.disposed)
            {
                this.disposed = true;

                this.Kubernetes.Dispose();
            }
        }
    }
}
