// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.V2.Models.Validation
{
    public class ResourceTypeRegexValidator : IValidator<string, IReadOnlyList<ErrorDetail>>
    {
        private readonly static JsonPointer Target = JsonPointer.Create("type");

        private readonly Regex typePattern;
        private readonly string? customErrorMessage;

        public ResourceTypeRegexValidator(Regex typePattern, string? customErrorMessage = null)
        {
            this.typePattern = typePattern;
            this.customErrorMessage = customErrorMessage;
        }

        public IReadOnlyList<ErrorDetail> Validate(string value)
        {
            if (this.typePattern.IsMatch(value))
            {
                return Array.Empty<ErrorDetail>();
            }

            return new[]
            {
                new ErrorDetail("InvalidType", this.customErrorMessage ?? $"Expected type to match the regular expression {this.typePattern}.", Target)
            };
        }
    }
}
