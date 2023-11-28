// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Utils;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models
{
    public readonly partial record struct K8sResourceType(string Group, string Version, string Kind)
    {
        public string ApiVersion => this.Group is "" ? this.Version : $"{this.Group}/{this.Version}";

        public static implicit operator string(K8sResourceType type) => type.ToString();

        public static implicit operator K8sResourceType(string value) => Parse(value);

        public static K8sResourceType Parse(string value)
        {
            var match = Pattern().Match(value);

            ArgumentExceptionHelper.ThrowIf(!match.Success);

            var group = match.Groups["group"].Value;
            var version = match.Groups["version"].Value;
            var kind = match.Groups["kind"].Value;

            return new(group, version, kind);
        }

        [GeneratedRegex(@"^((?<group>[a-zA-Z0-9.]+)\/)?(?<kind>[a-zA-Z]+)@(?<version>[a-zA-Z0-9]+)$")]
        public static partial Regex Pattern();

        public override string ToString() => this.Group is ""
            ? $"{this.Kind}@{this.Version}"
            : $"{this.Group}/{this.Kind}@{this.Version}";
    }
}
