// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models.Validation
{
    public class PassthroughResourceConfigValidator : IResourceConfigValidator
    {
        private static readonly Lazy<PassthroughResourceConfigValidator> LazyInstance = new(() => new());

        private PassthroughResourceConfigValidator() { }

        public static PassthroughResourceConfigValidator Instance => LazyInstance.Value;

        public IReadOnlyList<ErrorDetail> Validate(JsonObject? value) => Array.Empty<ErrorDetail>();
    }
}
