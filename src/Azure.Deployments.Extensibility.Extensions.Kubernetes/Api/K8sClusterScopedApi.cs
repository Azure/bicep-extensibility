// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using Json.Pointer;
using k8s.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Api
{
    public class K8sClusterScopedApi : K8sApi
    {
        public K8sClusterScopedApi(IK8sClient client, V1APIResource apiResource)
            : base(client, apiResource)
        {
        }

        public override async Task<K8sObject> PatchObjectAsync(K8sObject k8sObject, bool dryRun, CancellationToken cancellationToken)
        {
            this.EnsureNamespaceNotSpecified(k8sObject.Namespace);

            var patchedBody = await this.Client.PatchClusterScopedObjectAsync<JsonObject>(
                k8sObject.Body,
                this.Group,
                this.Version,
                this.Plural,
                k8sObject.Name,
                dryRun: dryRun,
                cancellationToken: cancellationToken);

            return new K8sObject(k8sObject.GroupVersionKind, patchedBody);
        }

        public override async Task<K8sObject?> GetObjectAsync(K8sObjectIdentifiers identifiers, CancellationToken cancellationToken)
        {
            this.EnsureNamespaceNotSpecified(identifiers.Namespace);

            var body = await this.Client.GetClusterScopedObjectAsync<JsonObject>(
                this.Group,
                this.Version,
                this.Plural,
                identifiers.Name,
                cancellationToken: cancellationToken);

            return body is not null
                ? new K8sObject(new GroupVersionKind(this.Group, this.Version, this.Kind), body)
                : null;
        }

        public override async Task DeleteObjectAsync(K8sObjectIdentifiers identifiers, CancellationToken cancellationToken)
        {
            this.EnsureNamespaceNotSpecified(identifiers.Namespace);

            await this.Client.DeleteClusterScopedObjectAsync(
                this.Group,
                this.Version,
                this.Plural,
                identifiers.Name,
                cancellationToken: cancellationToken);
        }

        private void EnsureNamespaceNotSpecified(string? @namespace)
        {
            if (@namespace is not null)
            {
                throw new ErrorResponseException(
                    "NamespaceNotAllowed",
                    $"Namespace must not be specified for cluster-scoped kind '{this.GroupKind}'.",
                    JsonPointer.Create("properties", "metadata", "namespace"));
            }
        }
    }
}
