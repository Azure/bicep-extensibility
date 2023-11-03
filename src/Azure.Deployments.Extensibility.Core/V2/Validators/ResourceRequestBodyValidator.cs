// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public class ResourceRequestBodyValidator : IValidator<ResourceRequestBody, Error?>
    {
        private class PassthroughConfigValidator : IValidator<JsonObject?, IReadOnlyList<ErrorDetail>>
        {
            private static readonly Lazy<PassthroughConfigValidator> LazyInstance = new(() => new());

            private PassthroughConfigValidator() { }

            public static PassthroughConfigValidator Instance => LazyInstance.Value;

            public IReadOnlyList<ErrorDetail> Validate(JsonObject? value) => Array.Empty<ErrorDetail>();
        }

        private readonly IValidator<string, IReadOnlyList<ErrorDetail>> typeValidator;
        private readonly Func<string, IValidator<JsonObject, IReadOnlyList<ErrorDetail>>> propertiesValidatorSelector;
        private readonly IValidator<JsonObject?, IReadOnlyList<ErrorDetail>> configValidator;

        public ResourceRequestBodyValidator(
            IValidator<string, IReadOnlyList<ErrorDetail>> typeValidator,
            IValidator<JsonObject, IReadOnlyList<ErrorDetail>> propertiesValidator,
            IValidator<JsonObject?, IReadOnlyList<ErrorDetail>>? configValidator = null)
            : this(typeValidator, _ => propertiesValidator, configValidator)
        {
        }

        public ResourceRequestBodyValidator(
            IValidator<string, IReadOnlyList<ErrorDetail>> typeValidator,
            Func<string, IValidator<JsonObject, IReadOnlyList<ErrorDetail>>> propertiesValidatorSelector,
            IValidator<JsonObject?, IReadOnlyList<ErrorDetail>>? configValidator = null)
        {
            this.typeValidator = typeValidator;
            this.propertiesValidatorSelector = propertiesValidatorSelector;
            this.configValidator = configValidator ?? PassthroughConfigValidator.Instance;
        }

        public virtual Error? Validate(ResourceRequestBody resourceRequestBody)
        {
            var configErrorDetails = this.configValidator.Validate(resourceRequestBody.Config);
            var typeErrorDetails = this.typeValidator.Validate(resourceRequestBody.Type);

            if (!typeErrorDetails.Any())
            {
                var propertiesValidator = this.propertiesValidatorSelector(resourceRequestBody.Type);
                var propertiesErrorDetails = propertiesValidator.Validate(resourceRequestBody.Properties);

                if (configErrorDetails.Any() || propertiesErrorDetails.Any())
                {
                    return Aggregate(configErrorDetails.Concat(propertiesErrorDetails), configErrorDetails.Count + propertiesErrorDetails.Count);
                }

                return null;
            }

            if (configErrorDetails.Any())
            {
                return Aggregate(configErrorDetails.Concat(typeErrorDetails), configErrorDetails.Count + typeErrorDetails.Count);
            }

            return null;
        }

        private static Error Aggregate(IEnumerable<ErrorDetail> errorDetails, int errorDetailsCount) => errorDetailsCount switch
        {
            1 => errorDetails.Single().AsError(),
            > 1 => new Error("MultipleErrorOccurred", "Multiple error occurred. Please see details.", details: errorDetails.ToArray()),
            _ => throw new InvalidOperationException($"Expected {nameof(errorDetails)} to contain at least one item."),
        };
    }
}
