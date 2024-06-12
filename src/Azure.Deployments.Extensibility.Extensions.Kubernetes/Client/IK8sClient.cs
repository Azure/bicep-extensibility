// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Client
{
    public interface IK8sClient : IDisposable
    {
        string ServerHost { get; }

        string? DefaultNamespace { get; }

        Task<VersionInfo> GetServerVersionInfoAsync(CancellationToken cancellationToken);

        Task<V1APIResourceList?> ListApiResourceAsync(string? group, string version, CancellationToken cancellationToken);

        Task<V1Namespace?> GetNamespaceAsync(string namespaceName, CancellationToken cancellationToken);

        Task<T> PatchClusterScopedObjectAsync<T>(JsonObject @object, string? group, string version, string plural, string objectName, bool dryRun, CancellationToken cancellationToken);

        Task<T> PatchNamespacedObjectAsync<T>(JsonObject @object, string? group, string version, string @namespace, string plural, string objectName, bool dryRun, CancellationToken cancellationToken);

        Task<T?> GetClusterScopedObjectAsync<T>(string? group, string version, string plural, string objectName, CancellationToken cancellationToken);

        Task<T?> GetNamespacedObjectAsync<T>(string? group, string version, string @namespace, string plural, string objectName, CancellationToken cancellationToken);

        Task DeleteClusterScopedObjectAsync(string? group, string version, string plural, string objectName, CancellationToken cancellationToken);

        Task DeleteNamespacedObjectAsync(string? group, string version, string @namespace, string plural, string objectName, CancellationToken cancellationToken);
    }
}
