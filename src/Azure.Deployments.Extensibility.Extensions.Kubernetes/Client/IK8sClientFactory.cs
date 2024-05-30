// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Client
{
    public interface IK8sClientFactory
    {
        public Task<IK8sClient> CreateAsync(JsonObject? config);
    }
}
