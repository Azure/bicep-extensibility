// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Models
{
    internal class K8sObject
    {
        public K8sObject(GroupVersionKind groupVersionKind, JsonObject body)
        {
            this.GroupVersionKind = groupVersionKind;
            this.Body = body;

            body["apiVersion"] = groupVersionKind.GroupVersion;
            body["kind"] = groupVersionKind.Kind;
        }

        public GroupVersionKind GroupVersionKind { get; }

        public JsonObject Body { get; }

        public JsonObject Metadata => this.Body["metadata"]?.AsObject() ?? throw new InvalidOperationException("Metadata must be non-null.");

        public string Name => this.Metadata["name"]?.GetValue<string>() ?? throw new InvalidOperationException("Name metadata must be non-null.");

        public string? Namespace => this.Metadata["namespace"]?.GetValue<string>();
    }
}
