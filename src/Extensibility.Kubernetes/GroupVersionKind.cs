// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Extensibility.Kubernetes
{
    // HEY LISTEN: no records on ns2.0 :-/
    public readonly struct GroupVersionKind : IEquatable<GroupVersionKind>
    {
        // Expected format = (<group>/)?<Kind>@<version>
        // ex:
        //
        //  apps/Deployment@v1 -> Kind: Deployment, ApiVersion: apps/v1
        //  Service@v1 -> Kind: Service, ApiVersion: v1
        private static readonly Regex parser = new Regex(@"^((?<group>[\w.]+)/)?(?<kind>[\w]+)@(?<version>[\w\d]+)$", RegexOptions.Compiled);

        // HEY LISTEN: nullable has partial support with ns2.0
        public static bool TryParse(string version, [NotNullWhen(true)] out GroupVersionKind? gvk)
        {
            if (parser.Match(version) is Match match)
            {
                var parsedGvk = new GroupVersionKind(match.Groups["group"].Value, match.Groups["version"].Value, match.Groups["kind"].Value);

                if (parsedGvk.Group != null && parsedGvk.Group.StartsWith("kubernetes."))
                {
                    // Remove the kubernetes. prefix for compat with radius.
                    parsedGvk = new GroupVersionKind(parsedGvk.Group.Substring("kubernetes.".Length), parsedGvk.Version, parsedGvk.Kind);
                }

                if (parsedGvk.Group == "core" || parsedGvk.Group == "")
                {
                    parsedGvk = new GroupVersionKind(null, parsedGvk.Version, parsedGvk.Kind);
                }

                gvk = parsedGvk;
                return true;
            }

            gvk = null;
            return false;
        }

        public GroupVersionKind(string? group, string version, string kind)
        {
            this.Group = group;
            this.Version = version;
            this.Kind = kind;
        }

        public string ApiVersion => Group is null ? Version : $"{Group}/{Version}";

        public string? Group { get; }

        public string Version { get; }

        public string Kind { get; }

        public override string ToString()
        {
            return $"{Group}/{Version} {Kind}";
        }

        public bool Equals(GroupVersionKind other)
        {
            return
                string.Equals(Group, other.Group, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Kind, other.Kind, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is GroupVersionKind gvk && Equals(gvk);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Group, StringComparer.OrdinalIgnoreCase);
            hash.Add(Version, StringComparer.OrdinalIgnoreCase);
            hash.Add(Kind, StringComparer.OrdinalIgnoreCase);
            return hash.GetHashCode();
        }
    }
}
