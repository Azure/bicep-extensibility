// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Constants;
using k8s.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models
{
    public record K8sResource
    {
        [SetsRequiredMembers]
        public K8sResource(K8sResourceReferenceId referenceId, K8sResourceType type, JsonObject properties)
        {
            this.ReferenceId = referenceId;
            this.Type = type;
            this.Properties = properties;

            // Patch properties.
            this.Properties["apiVersion"] = type.ApiVersion;
            this.Properties["kind"] = type.Kind;
        }

        public required K8sResourceReferenceId ReferenceId { get; init; }

        public required K8sResourceType Type { get; init; }

        public required JsonObject Properties { get; init; }

        public string? Namespace => this.ReferenceId.Namespace;

        public string Name => this.ReferenceId.Name;

        public string Group => this.Type.Group;

        public string Version => this.Type.Version;

        public string Kind => this.Type.Kind;

        public V1Patch ToV1Patch()
        {
            var propertiesContent = JsonSerializer.Serialize(this.Properties, JsonDefaults.SerializerOptions);

            return new(propertiesContent, V1Patch.PatchType.ApplyPatch);
        }
    }
}
