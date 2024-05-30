// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Models
{
    public readonly record struct GroupVersionKind(string? Group, string Version, string Kind)
    {
        public string GroupVersion => string.IsNullOrEmpty(this.Group) ? this.Version : $"{this.Group}/{this.Version}";
    }
}
