// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Validators
{
    public class ResourceRequestBodyValidator(
        IResourceTypeValidator typeValidator,
        Func<string, IResourcePropertiesValidator> propertiesValidatorSelector,
        IResourceConfigValidator? configValidator = null) : IResourceRequestBodyValidator
    {
        private readonly IResourceConfigValidator configValidator = configValidator ?? PassthroughResourceConfigValidator.Instance;

        public ResourceRequestBodyValidator(
            IResourceTypeValidator typeValidator,
            IResourcePropertiesValidator propertiesValidator,
            IResourceConfigValidator? configValidator = null)
            : this(typeValidator, _ => propertiesValidator, configValidator)
        {
        }

        public virtual Error? Validate(ResourceRequestBody resourceRequestBody)
        {
            var typeErrorDetails = typeValidator.Validate(resourceRequestBody.Type);
            var propertiesErrorDetails = propertiesValidatorSelector(resourceRequestBody.Type).Validate(resourceRequestBody.Properties);
            var configErrorDetails = this.configValidator.Validate(resourceRequestBody.Config);

            return Aggregate(
                typeErrorDetails.Concat(propertiesErrorDetails).Concat(configErrorDetails),
                typeErrorDetails.Count + propertiesErrorDetails.Count + configErrorDetails.Count);
        }

        private static Error? Aggregate(IEnumerable<ErrorDetail> errorDetails, int errorDetailsCount) => errorDetailsCount switch
        {
            0 => null,
            1 => errorDetails.Single().AsError(),
            _ => new Error("MultipleErrorOccurred", "Multiple error occurred. Please see details for more information.", details: errorDetails.ToArray()),
        };
    }
}
