// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validation.Rules
{
    public class MatchRegexCriterion<TModel>(Regex regex) : IPropertyRuleCriterion<TModel, string?>
    {
        public string ErrorCode { get; set; } = "RegularExpressionMismatch";

        public string ErrorMessage { get; set; } = $"Value does not match the regular expression /{regex}/.";

        public IEnumerable<ErrorDetail> Evaluate(TModel model, string? propertyValue, JsonPointer propertyPointer)
        {
            ArgumentNullException.ThrowIfNull(propertyValue, nameof(propertyValue));

            if (!regex.IsMatch(propertyValue))
            {
                yield return new(this.ErrorCode, this.ErrorMessage, propertyPointer);
            }
        }
    }
}
