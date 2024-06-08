// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation;
using Semver;
using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Api.ApiCatalog
{
    public record K8sApiMetadata(
        string? Group,
        string Version,
        string Kind,
        string Plural,
        bool Namespaced,
        ImmutableArray<string> MajorMinorServerVersions) : IComparable<K8sApiMetadata>
    {
        public K8sApiMetadata(string? group, string version, string kind)
            : this(group, version, kind, "", default, default)
        {
        }

        public string ApiVersion => !string.IsNullOrEmpty(this.Group) ? $"{this.Group}/{this.Version}" : this.Version;

        public static K8sApiMetadata From(ResourceSpecification resourceSpecification)
        {
            ArgumentException.ThrowIfNullOrEmpty(resourceSpecification.ApiVersion);

            var typeMatch = RegexPatterns.ResourceType().Match(resourceSpecification.Type);
            var group = typeMatch.Groups["group"].Value;
            var kind = typeMatch.Groups["kind"].Value;
            var version = resourceSpecification.ApiVersion;

            return new(group, version, kind, "", default, default);
        }

        public int CompareTo(K8sApiMetadata? other)
        {
            if (other is null)
            {
                return 1;
            }

            int result = string.CompareOrdinal(this.Group ?? "", other.Group ?? "");

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

        public bool Matches(SemVersion serverVersion)
        {
            var majorMinorServerVersion = $"{serverVersion.Major}.{serverVersion.Major}";

            return this.MajorMinorServerVersions.BinarySearch(majorMinorServerVersion) >= 0;
        }
    }
}
