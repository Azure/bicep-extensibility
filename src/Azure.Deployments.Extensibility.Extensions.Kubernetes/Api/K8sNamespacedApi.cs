// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using Json.Pointer;
using k8s.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Api
{
    internal class K8sNamespacedApi : K8sApi
    {
        public K8sNamespacedApi(IK8sClient client, V1APIResource apiResource)
            : base(client, apiResource)
        {
        }

        public override async Task<K8sObject> PatchObjectAsync(K8sObject k8sObject, bool dryRun, CancellationToken cancellationToken)
        {
            var @namespace = k8sObject.Namespace ?? this.Client.DefaultNamespace;

            this.EnsureNamespaceSpecified(@namespace);

            var patchedBody = await this.Client.PatchNamespacedObjectAsync<JsonObject>(
                k8sObject.Body,
                this.Group,
                this.Version,
                @namespace,
                this.Plural,
                k8sObject.Name,
                dryRun: dryRun,
                cancellationToken: cancellationToken);

            return new K8sObject(k8sObject.GroupVersionKind, patchedBody);
        }

        public override async Task<K8sObject?> GetObjectAsync(K8sObjectIdentifiers identifiers, CancellationToken cancellationToken)
        {
            var @namespace = identifiers.Namespace ?? this.Client.DefaultNamespace;

            this.EnsureNamespaceSpecified(@namespace);

            var properties = await this.Client.GetNamespacedObjectAsync<JsonObject>(
                this.Group,
                this.Version,
                @namespace,
                this.Plural,
                identifiers.Name,
                cancellationToken: cancellationToken);

            return properties is not null
                ? new K8sObject(new GroupVersionKind(this.Group, this.Version, this.Kind), properties)
                : null;
        }

        public override Task DeleteObjectAsync(K8sObjectIdentifiers identifiers, CancellationToken cancellationToken)
        {
            // The identifiers must include the namespace of object to delete.
            // The namespace was set by the CreateOrUpdateResource API.
            // Client.DefaultNamespace cannot be used because it might be different than
            // the namespace used when creating the object.
            this.EnsureNamespaceSpecified(identifiers.Namespace);

            return this.Client.DeleteNamespacedObjectAsync(
                this.Group,
                this.Version,
                identifiers.Namespace,
                this.Plural,
                identifiers.Name,
                cancellationToken: cancellationToken);
        }

        private void EnsureNamespaceSpecified([NotNull]string? @namespace)
        {
            if (@namespace is null)
            {
                throw new ErrorResponseException(
                    "MissingNamespace",
                    $"Namespace is required for namespaced kind '{this.GroupKind}'.",
                    JsonPointer.Create("properties", "metadata"));
            }
        }

    }
}
