// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models.Validation
{
    public class PassthroughResourcePropertiesValidator : IResourcePropertiesValidator
    {
        private static readonly Lazy<PassthroughResourcePropertiesValidator> LazyInstance = new(() => new());

        private PassthroughResourcePropertiesValidator() { }

        public static PassthroughResourcePropertiesValidator Instance => LazyInstance.Value;

        public IReadOnlyList<ErrorDetail> Validate(JsonObject value) => Array.Empty<ErrorDetail>();
    }
}
