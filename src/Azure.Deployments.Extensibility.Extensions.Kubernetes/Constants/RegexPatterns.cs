// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation
{
    internal partial class RegexPatterns
    {
        [GeneratedRegex(@"^((?<group>[a-zA-Z0-9.]+)\/)?(?<kind>[a-zA-Z]+)$")]
        public static partial Regex ResourceType();

        [GeneratedRegex("[a-zA-Z0-9]+")]
        public static partial Regex ApiVersion();
    }
}
