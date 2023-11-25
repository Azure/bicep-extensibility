// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validators
{
    public class ResourceTypeRegexValidator(Regex typePattern, string? customErrorMessage = null) : IResourceTypeValidator
    {
        private readonly static JsonPointer Target = JsonPointer.Create("type");

        public virtual IReadOnlyList<ErrorDetail> Validate(string value)
        {
            if (typePattern.IsMatch(value))
            {
                return Array.Empty<ErrorDetail>();
            }

            return new[]
            {
                new ErrorDetail("InvalidType", customErrorMessage ?? $"Expected type to match the regular expression {typePattern}.", Target)
            };
        }
    }
}
