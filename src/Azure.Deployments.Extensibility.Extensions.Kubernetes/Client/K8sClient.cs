// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Json;
using k8s;
using k8s.Autorest;
using k8s.Models;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Client
{
    internal class K8sClient : IK8sClient
    {
        private readonly IKubernetes kubernetes;

        public K8sClient(KubernetesClientConfiguration clientConfiguration)
        {
            this.kubernetes = new k8s.Kubernetes(clientConfiguration);
            this.ServerHost = this.kubernetes.BaseUri.Host.ToLowerInvariant();
            this.DefaultNamespace = clientConfiguration.Namespace;
        }

        public string ServerHost { get; }

        public string? DefaultNamespace { get; }

        public void Dispose() => this.kubernetes.Dispose();

        public async Task<VersionInfo> GetServerVersionInfoAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await this.kubernetes.Version.GetCodeAsync(cancellationToken);
            }
            catch (HttpOperationException exception)
            {
                throw ConvertToErrorResponseException(exception);
            }
        }

        public async Task<V1APIResourceList?> ListApiResourceAsync(string? group, string version, CancellationToken cancellationToken)
        {
            try
            {
                var genericClient = new GenericClient(this.kubernetes, group, version, plural: "", disposeClient: false);

                return await genericClient.ListAsync<V1APIResourceList>(cancellationToken);
            }
            catch (HttpOperationException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (HttpOperationException exception)
            {
                throw ConvertToErrorResponseException(exception);
            }
        }


        public async Task<V1Namespace?> GetNamespaceAsync(string? namespaceName, CancellationToken cancellationToken)
        {
            try
            {
                return await this.kubernetes.CoreV1.ReadNamespaceAsync(namespaceName, cancellationToken: cancellationToken);
            }
            catch (HttpOperationException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (HttpOperationException exception)
            {
                throw ConvertToErrorResponseException(exception);
            }
        }

        public async Task<T> PatchClusterScopedObjectAsync<T>(JsonObject @object, string? group, string version, string plural, string objectName, bool dryRun, CancellationToken cancellationToken)
        {
            try
            {
                return await this.kubernetes.CustomObjects.PatchClusterCustomObjectAsync<T>(
                    CreateApplyPatchFor(@object),
                    group,
                    version,
                    plural,
                    objectName,
                    dryRun: dryRun ? "All" : null,
                    fieldManager: "Bicep",
                    cancellationToken: cancellationToken);
            }
            catch (HttpOperationException exception)
            {
                throw ConvertToErrorResponseException(exception);
            }
        }

        public async Task<T> PatchNamespacedObjectAsync<T>(JsonObject @object, string? group, string version, string @namespace, string plural, string objectName, bool dryRun, CancellationToken cancellationToken)
        {
            try
            {
                return await this.kubernetes.CustomObjects.PatchNamespacedCustomObjectAsync<T>(
                    CreateApplyPatchFor(@object),
                    group,
                    version,
                    @namespace,
                    plural,
                    objectName,
                    dryRun: dryRun ? "All" : null,
                    fieldManager: "Bicep",
                    cancellationToken: cancellationToken);
            }
            catch (HttpOperationException exception)
            {
                throw ConvertToErrorResponseException(exception);
            }
        }

        public async Task<T?> GetClusterScopedObjectAsync<T>(string? group, string version, string plural, string objectName, CancellationToken cancellationToken)
        {
            try
            {
                return await this.kubernetes.CustomObjects.GetClusterCustomObjectAsync<T>(group, version, plural, objectName, cancellationToken: cancellationToken);
            }
            catch (HttpOperationException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }
            catch (HttpOperationException exception)
            {
                throw ConvertToErrorResponseException(exception);
            }
        }

        public async Task<T?> GetNamespacedObjectAsync<T>(string? group, string version, string @namespace, string plural, string objectName, CancellationToken cancellationToken)
        {
            try
            {
                return await this.kubernetes.CustomObjects.GetNamespacedCustomObjectAsync<T>(group, version, @namespace, plural, objectName, cancellationToken: cancellationToken);
            }
            catch (HttpOperationException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }
            catch (HttpOperationException exception)
            {
                throw ConvertToErrorResponseException(exception);
            }
        }

        public async Task DeleteClusterScopedObjectAsync(string? group, string version, string plural, string objectName, CancellationToken cancellationToken)
        {
            try
            {
                await this.kubernetes.CustomObjects.DeleteClusterCustomObjectAsync(group, version, plural, objectName, cancellationToken: cancellationToken);
            }
            catch (HttpOperationException exception)
            {
                throw ConvertToErrorResponseException(exception);
            }
        }

        public async Task DeleteNamespacedObjectAsync(string? group, string version, string @namespace, string plural, string objectName, CancellationToken cancellationToken)
        {
            try
            {
                await this.kubernetes.CustomObjects.DeleteNamespacedCustomObjectAsync(group, version, @namespace, plural, objectName, cancellationToken: cancellationToken);
            }
            catch (HttpOperationException exception)
            {
                throw ConvertToErrorResponseException(exception);
            }
        }

        private static V1Patch CreateApplyPatchFor(JsonObject @object)
        {
            var patchContent = JsonSerializer.Serialize(@object, JsonDefaults.SerializerContext.JsonObject);

            return new V1Patch(patchContent, V1Patch.PatchType.ApplyPatch);
        }

        private static ErrorResponseException ConvertToErrorResponseException(HttpOperationException exception) => new("KubernetesOperationFailure", exception.Message);
    }
}
