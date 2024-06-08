// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Api.ApiCatalog;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using Json.Pointer;
using k8s.Models;
using Semver;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Api
{
    internal class K8sApiDiscovery
    {
        private readonly IK8sClient client;

        public K8sApiDiscovery(IK8sClient client)
        {
            this.client = client;
        }

        public async Task<K8sApi> FindApiAsync(GroupVersionKind groupVersionKind, CancellationToken cancellationToken)
        {
            var apiResource = await this.FindApiResourceAsync(groupVersionKind, cancellationToken);

            return apiResource.Namespaced
                ? new K8sNamespacedApi(this.client, apiResource)
                : new K8sClusterScopedApi(this.client, apiResource);
        }

        private async Task<V1APIResource> FindApiResourceAsync(GroupVersionKind groupVersionKind, CancellationToken cancellationToken)
        {
            var (group, version, kind) = groupVersionKind;

            if (await this.TryFastFindApiResourceAsync(groupVersionKind, cancellationToken) is { } apiResource)
            {
                return apiResource;
            }

            var apiResourceList = await this.client.ListApiResourceAsync(group, version, cancellationToken);

            if (apiResourceList is null)
            {
                throw new ErrorResponseException("UnknownResourceType", $"Unknown API group version '{group}/{version}'.");
            }

            return apiResourceList.Resources.SingleOrDefault(x => x.Kind.Equals(kind, StringComparison.Ordinal)) ??
                throw new ErrorResponseException("UnknownResourceKind", $"Unknown resource kind '{kind}'.", JsonPointer.Create("type"));
        }

        private async Task<V1APIResource?> TryFastFindApiResourceAsync(GroupVersionKind groupVersionKind, CancellationToken cancellationToken)
        {
            var (group, version, kind) = groupVersionKind;

            if (K8sApiCatalog.Instance.TryFindMatchingRecord(new(group, version, kind)) is { } apiMetadata)
            {
                var serverVersionInfo = await this.client.GetServerVersionInfoAsync(cancellationToken);
                var serverMajorVersion = int.Parse(serverVersionInfo.Major);
                var serverMinorVersion = int.Parse(serverVersionInfo.Minor);
                var serverMajorMinorVersion = new SemVersion(serverMajorVersion, serverMinorVersion);

                if (apiMetadata.Matches(serverMajorMinorVersion))
                {
                    return new V1APIResource
                    {
                        Group = group,
                        Version = version,
                        Kind = kind,
                        Name = apiMetadata.Plural,
                        Namespaced = apiMetadata.Namespaced,
                    };
                }
            }

            return null;
        }
    }
}
