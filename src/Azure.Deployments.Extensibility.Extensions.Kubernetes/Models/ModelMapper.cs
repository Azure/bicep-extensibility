// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Models
{
    internal static class ModelMapper
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

            group = group.Equals("core", StringComparison.OrdinalIgnoreCase) ? "" : group;

            return new(group, version, kind);
        }

        public static Resource MapToResource(K8sObjectIdentifiers identifiers, K8sObject k8sObject, string configId)
        {
            var (group, version, kind) = k8sObject.GroupVersionKind;
            var resourceType = string.IsNullOrEmpty(group) ? $"core/{kind}" : $"{group}/{kind}";

            return new Resource
            {
                Type = resourceType,
                ApiVersion = version,
                Identifiers = MapToResourceIdentifiers(identifiers),
                Properties = k8sObject.Body,
                ConfigId = configId
            };
        }

        public static K8sObjectIdentifiers MapToK8sObjectIdentifiers(JsonObject identifiers)
        {
            var metadataObject = identifiers["metadata"]?.AsObject() ?? throw new InvalidOperationException("Metadata must be non-null.");
            var name = metadataObject["name"]?.GetValue<string>() ?? throw new InvalidOperationException("Name must be non-null.");
            var @namespace = metadataObject["namespace"]?.GetValue<string>();

            return new(name, @namespace);
        }

        public static JsonObject MapToResourceIdentifiers(K8sObjectIdentifiers identifiers) => new()
        {
            ["metadata"] = new JsonObject()
            {
                ["name"] = identifiers.Name,
                ["namespace"] = identifiers.Namespace,
            }
        };
    }
}
