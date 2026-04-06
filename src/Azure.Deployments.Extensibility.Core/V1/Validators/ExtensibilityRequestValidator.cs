// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Exceptions;
using Json.Schema;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.Validators
{
    /// <summary>
    /// Validates both the import configuration and resource properties of a V1 extensibility operation request,
    /// throwing an <see cref="ExtensibilityException"/> if any validation errors are found.
    /// </summary>
    public class ExtensibilityRequestValidator
    {
        private readonly JsonSchema importConfigSchema;

        private readonly Regex resourceTypeRegex;

        private readonly JsonSchema resourcePropertySchema;

        /// <summary>
        /// Initializes a new instance with the schemas and type regex for validation.
        /// </summary>
        /// <param name="importConfigSchema">The JSON Schema for the import configuration.</param>
        /// <param name="resourceTypeRegex">A regex that the resource type must match.</param>
        /// <param name="resourcePropertySchema">The JSON Schema for the resource properties.</param>
        public ExtensibilityRequestValidator(JsonSchema importConfigSchema, Regex resourceTypeRegex, JsonSchema resourcePropertySchema)
        {
            this.importConfigSchema = importConfigSchema;
            this.resourceTypeRegex = resourceTypeRegex;
            this.resourcePropertySchema = resourcePropertySchema;
        }

        /// <summary>
        /// Validate the request and throw an <see cref="ExtensibilityException"/> if any validation errors are found.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <exception cref="ExtensibilityException">Thrown when validation fails.</exception>
        public void ValidateAndThrow(ExtensibilityOperationRequest request)
        {
            var importErrors = new ExtensibleImportValidator(this.importConfigSchema).Validate(request.Import);
            var resourceErrors = new ExtensibleResourceValidator(this.resourceTypeRegex, this.resourcePropertySchema).Validate(request.Resource);

            if (importErrors.Any() || resourceErrors.Any())
            {
                throw new ExtensibilityException(importErrors.Concat(resourceErrors));
            }
        }
    }
}
