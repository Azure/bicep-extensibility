// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Models
{
    public readonly partial record struct KubernetesResourceType(string Group, string Version, string Kind)
    {
        public readonly static Regex Regex = TypePattern();

        public string ApiVersion => this.Group == "" ? this.Version : $"{this.Group}/{this.Version}";

        public static KubernetesResourceType Parse(string rawType)
        {
            if (Regex.Match(rawType) is not { } match)
            {
                throw new ArgumentException($"Expected {nameof(rawType)} to be valid.");
            }

            var parsedType = new KubernetesResourceType(match.Groups["group"].Value, match.Groups["version"].Value, match.Groups["kind"].Value);

            if (parsedType.Group != null && parsedType.Group.StartsWith("kubernetes."))
            {
                parsedType = new KubernetesResourceType(parsedType.Group["kubernetes.".Length..], parsedType.Version, parsedType.Kind);
            }

            if (parsedType.Group == "core")
            {
                parsedType = new KubernetesResourceType("", parsedType.Version, parsedType.Kind);
            }

            return parsedType;
        }

        [GeneratedRegex(@"^((?<group>[\w-.]+)/)?(?<kind>[\w-.]+)@(?<version>[\w\d]+)$", RegexOptions.Compiled)]
        private static partial Regex TypePattern();
    }
}
