// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models
{
    public record K8sApiMetadata(
        string? Group,
        string Version,
        string Kind,
        string Plural,
        bool Namespaced,
        ImmutableArray<string> MajorMinorServerVersions) : IComparable<K8sApiMetadata>
    {
        public int CompareTo(K8sApiMetadata? other)
        {
            if (other is null)
            {
                return 1;
            }

            int result = string.IsNullOrEmpty(this.Group) && string.IsNullOrEmpty(other.Group)
                ? 0
                : string.CompareOrdinal(this.Group, other.Group);

            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(this.Version, other.Version);

            if (result != 0)
            {
                return result;
            }

            return string.CompareOrdinal(this.Kind, other.Kind);
        }
    }
}
