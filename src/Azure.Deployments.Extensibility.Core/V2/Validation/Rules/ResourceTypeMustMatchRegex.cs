// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validation.Rules
{
    public class ResourceTypeMustMatchRegex : ModelValidationRuleContext, IModelValidationRule<ResourceSpecification>, IModelValidationRule<ResourceReference>
    {
        private readonly static JsonPointer TypePointer = JsonPointer.Create("type");

        public Regex? TypePattern { get; set; }

        public IEnumerable<ErrorDetail> Validate(ResourceSpecification resourceDefinition) => this.ValidateType(resourceDefinition.Type);

        public IEnumerable<ErrorDetail> Validate(ResourceReference resourceReference) => this.ValidateType(resourceReference.Type);

        public IEnumerable<ErrorDetail> ValidateType(string type)
        {
            ArgumentNullException.ThrowIfNull(this.TypePattern, nameof(this.TypePattern));

            if (!this.TypePattern.IsMatch(type))
            {
                yield return new("InvalidType", $"Expected the resource type '{type}' to match the regular expression {this.TypePattern}.", TypePointer);
            }
        }
    }
}
