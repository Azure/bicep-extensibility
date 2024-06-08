// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Models
{
    public static class ModelMapper
    {
        public static GroupVersionKind MapToGroupVersionKind(string resourceType, string? resourceApiVersion)
        {
            ArgumentException.ThrowIfNullOrEmpty(resourceApiVersion);

            var typeMatch = RegexPatterns.ResourceType().Match(resourceType);

            if (!typeMatch.Success)
            {
                throw new InvalidOperationException($"Expected {nameof(resourceType)} to be validated.");
            }

            var group = typeMatch.Groups["group"].Value;
            var kind = typeMatch.Groups["kind"].Value;
            var version = resourceApiVersion;

            return new(group, version, kind);
        }

        public static Resource MapToResource(K8sObjectIdentifiers identifiers, K8sObject k8sObject)
        {
            var (group, version, kind) = k8sObject.GroupVersionKind;
            var resourceType = string.IsNullOrEmpty(group) ? $"core/{kind}" : $"{group}/{kind}";

            return new Resource
            {
                Type = resourceType,
                ApiVersion = version,
                Identifiers = MapToResourceIdentifiers(identifiers),
                Properties = k8sObject.Body,
            };
        }

        public static K8sObjectIdentifiers MapToK8sObjectIdentifiers(JsonObject identifiers)
        {
            var metadataObject = identifiers["metadata"]?.AsObject() ?? throw new InvalidOperationException("Metadata must be non-null.");
            var name = metadataObject["name"]?.GetValue<string>() ?? throw new InvalidOperationException("Name must be non-null.");
            var @namespace = metadataObject["namespace"]?.GetValue<string>();
            var serverHostHash = identifiers["serverHostHash"]?.GetValue<string>();

            return new(name, @namespace, serverHostHash);
        }

        public static JsonObject MapToResourceIdentifiers(K8sObjectIdentifiers identifiers) => new()
        {
            ["metadata"] = new JsonObject()
            {
                ["name"] = identifiers.Name,
                ["namespace"] = identifiers.Namespace,
            },
            ["serverHostHash"] = identifiers.ServerHostHash,
        };
    }
}
