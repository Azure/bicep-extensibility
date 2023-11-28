// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Path;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Extensions
{
    public static class ResourceRequestBodyExtensions
    {
        public readonly static JsonPointer NamePointer = JsonPointer.Create("metadata", "name");

        public readonly static JsonPointer NamespacePointer = JsonPointer.Create("metadata", "namespace");

        public static string GetName(this ResourceRequestBody resourceRequestBody)
        {
            if (NamePointer.TryEvaluate(resourceRequestBody.Properties, out var nameNode) &&
                nameNode is not null &&
                nameNode.TryGetValue<string>(out var name))
            {
                return name;
            }

            throw new InvalidOperationException("Expected name to exist.");
        }

        public static string? TryGetNamespace(this ResourceRequestBody resourceRequestBody)
        {
            if (NamespacePointer.TryEvaluate(resourceRequestBody.Properties, out var namespaceNode) &&
                namespaceNode is not null &&
                namespaceNode.TryGetValue<string>(out var @namespace))
            {
                return @namespace;
            }

            return null;
        }
    }
}
