// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models.Validation
{
    public class ResourceRequestBodyValidator : IResourceRequestBodyValidator
    {
        private readonly IResourceTypeValidator typeValidator;
        private readonly Func<string, IResourcePropertiesValidator> propertiesValidatorSelector;
        private readonly IResourceConfigValidator configValidator;

        public ResourceRequestBodyValidator(
            IResourceTypeValidator typeValidator,
            IResourcePropertiesValidator propertiesValidator,
            IResourceConfigValidator? configValidator = null)
            : this(typeValidator, _ => propertiesValidator, configValidator)
        {
        }

        public ResourceRequestBodyValidator(
            IResourceTypeValidator typeValidator,
            Func<string, IResourcePropertiesValidator> propertiesValidatorSelector,
            IResourceConfigValidator? configValidator = null)
        {
            this.typeValidator = typeValidator;
            this.propertiesValidatorSelector = propertiesValidatorSelector;
            this.configValidator = configValidator ?? PassthroughResourceConfigValidator.Instance;
        }

        public virtual Error? Validate(ResourceRequestBody resourceRequestBody)
        {
            var typeErrorDetails = this.typeValidator.Validate(resourceRequestBody.Type);
            var propertiesErrorDetails = this.propertiesValidatorSelector(resourceRequestBody.Type).Validate(resourceRequestBody.Properties);
            var configErrorDetails = this.configValidator.Validate(resourceRequestBody.Config);

            return Aggregate(
                typeErrorDetails.Concat(propertiesErrorDetails).Concat(configErrorDetails),
                typeErrorDetails.Count + propertiesErrorDetails.Count + configErrorDetails.Count);
        }

        private static Error? Aggregate(IEnumerable<ErrorDetail> errorDetails, int errorDetailsCount) => errorDetailsCount switch
        {
            0 => null,
            1 => errorDetails.Single().AsError(),
            _ => new Error("MultipleErrorOccurred", "Multiple error occurred. Please see details.", details: errorDetails.ToArray()),
        };
    }
}
