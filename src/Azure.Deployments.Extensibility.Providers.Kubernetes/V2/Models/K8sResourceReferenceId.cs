// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Extensions;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Utils;
using Json.Pointer;
using k8s.Models;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models
{
    public readonly record struct K8sResourceReferenceId(string Group, string Version, string Plural, string Kind, string? Namespace, string Name, string ClusterHostHash)
    {
        public string Value => HttpUtility.UrlEncode($"01-{this.Group}:{this.Version}:{this.Plural}:{this.Kind}:{this.Namespace}:{this.Name}:{this.ClusterHostHash}");

        public static implicit operator string(K8sResourceReferenceId referenceId) => referenceId.Value;

        public static implicit operator K8sResourceReferenceId(string value) => Parse(value);

        public static K8sResourceReferenceId Create(V1APIResource apiResource, K8sClusterAccessConfig config, ResourceRequestBody requestBody)
        {
            var @namespace = requestBody.TryGetNamespace();

            if (!apiResource.Namespaced && @namespace is not null)
            {
                throw new ErrorResponseException(
                    "NamespaceNotAllowed",
                    $"Namespace should be specified for a cluster-scoped resource kind '{apiResource.Kind}'.",
                    JsonPointer.Create("properties", "metadata", "namespace"));
            }

            @namespace ??= config.Namespace;

            if (apiResource.Namespaced && @namespace is null)
            {
                throw new ErrorResponseException(
                    "NamespaceNotSpecified",
                    $"Namespace is not specified for a namespaced resource kind '{apiResource.Kind}'.",
                    JsonPointer.Create("properties", "metadata", "namespace"));
            }

            var clusterHostBytes = Encoding.UTF8.GetBytes(config.ClientConfiguration.Host);
            var clusterHostHash = Convert.ToHexString(SHA256.HashData(clusterHostBytes));

            return new K8sResourceReferenceId
            {
                Group = apiResource.Group,
                Version = apiResource.Version,
                Plural = apiResource.Name,
                Kind = apiResource.Kind,
                Namespace = apiResource.Namespaced ? @namespace : null,
                Name = requestBody.GetName(),
                ClusterHostHash = clusterHostHash,
            };
        }

        public static K8sResourceReferenceId Parse(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            ArgumentExceptionHelper.ThrowIf(!value.StartsWith("01-"));

            value = value[3..];
            var parts = HttpUtility.UrlDecode(value).Split(':');

            ArgumentExceptionHelper.ThrowIf(parts.Length != 7);

            var group = parts[0];
            var version = parts[1];
            var plural = parts[2];
            var kind = parts[3];
            var @namespace = parts[4];
            var name = parts[5];
            var clusterHostHash = parts[6];

            ArgumentException.ThrowIfNullOrEmpty(version);
            ArgumentException.ThrowIfNullOrEmpty(plural);
            ArgumentException.ThrowIfNullOrEmpty(kind);
            ArgumentException.ThrowIfNullOrEmpty(@namespace);
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentException.ThrowIfNullOrEmpty(clusterHostHash);

            return new(group, version, plural, kind, @namespace, name, clusterHostHash);
        }

        public override string ToString() => this.Value;
    }
}
