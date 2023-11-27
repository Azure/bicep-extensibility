// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Exceptions;
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

        public void ValidateAndThrow(ResourceRequestBody requestBody)
        {
            if (this.Validate(requestBody) is { } error)
            {
                throw new ErrorResponseException(error);
            }
        }

        public virtual Error? Validate(ResourceRequestBody requestBody)
        {
            var typeErrorDetails = typeValidator.Validate(requestBody.Type);
            var propertiesErrorDetails = propertiesValidatorSelector(requestBody.Type).Validate(requestBody.Properties);
            var configErrorDetails = this.configValidator.Validate(requestBody.Config);

            return Aggregate(
                typeErrorDetails.Concat(propertiesErrorDetails).Concat(configErrorDetails),
                typeErrorDetails.Count + propertiesErrorDetails.Count + configErrorDetails.Count);
        }

        private static Error? Aggregate(IEnumerable<ErrorDetail> errorDetails, int errorDetailsCount) => errorDetailsCount switch
        {
            0 => null,
            1 => errorDetails.Single().AsError(),
            _ => new Error("MultipleErrorsOccurred", "Multiple errors occurred. Please see details for more information.", details: errorDetails.ToArray()),
        };
    }
}
