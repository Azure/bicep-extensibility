// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using k8s.Models;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Api
{
    public abstract class K8sApi
    {
        private readonly V1APIResource apiResource;

        protected K8sApi(IK8sClient client, V1APIResource apiResource)
        {
            this.Client = client;
            this.apiResource = apiResource;
        }

        public IK8sClient Client { get; }

        public bool Namespaced => this.apiResource.Namespaced;

        public string? Group => this.apiResource.Group;

        public string Version => this.apiResource.Version;

        public string Kind => this.apiResource.Kind;

        public string Plural => this.apiResource.Name;

        public string GroupKind => string.IsNullOrEmpty(this.Group) ? this.Kind : $"{this.Group}/{this.Kind}";

        public abstract Task<K8sObject> PatchObjectAsync(K8sObject k8sObject, bool dryRun, CancellationToken cancellationToken);

        public abstract Task<K8sObject?> GetObjectAsync(K8sObjectIdentifiers identifiers, CancellationToken cancellationToken);

        public abstract Task DeleteObjectAsync(K8sObjectIdentifiers identifiers, CancellationToken cancellationToken);
    }
}
