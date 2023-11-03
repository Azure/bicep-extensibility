// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public class ResourceTypeRegexValidator : IValidator<string, IReadOnlyList<ErrorDetail>>
    {
        private readonly static JsonPointer Target = JsonPointer.Create("type");

        private readonly Regex typePattern;
        private readonly string? errorMessage;

        public ResourceTypeRegexValidator(Regex typePattern, string? errorMessage = null)
        {
            this.typePattern = typePattern;
            this.errorMessage = errorMessage;
        }

        public IReadOnlyList<ErrorDetail> Validate(string value)
        {
            if (this.typePattern.IsMatch(value))
            {
                return Array.Empty<ErrorDetail>();
            }

            return new[]
            {
                new ErrorDetail("InvalidType", this.errorMessage ?? $"Expected type to match the regular expression {this.typePattern}.", Target)
            };
        }
    }
}
