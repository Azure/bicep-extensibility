// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Validators;
using Json.Schema;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ExtensibilityOperationRequest"/>.
    /// </summary>
    public static class ExtensibilityRequestExtensions
    {
        /// <summary>
        /// Validate the request against the specified schemas and convert it to a strongly-typed request.
        /// </summary>
        /// <typeparam name="TConfig">The concrete import configuration type.</typeparam>
        /// <typeparam name="TProperty">The concrete resource properties type.</typeparam>
        /// <param name="request">The request to validate and convert.</param>
        /// <param name="importConfigSchema">The JSON Schema for the import configuration.</param>
        /// <param name="resourceTypeRegex">A regex that the resource type must match.</param>
        /// <param name="resourcePropertySchema">The JSON Schema for the resource properties.</param>
        /// <returns>A strongly-typed operation request.</returns>
        /// <exception cref="Exceptions.ExtensibilityException">Thrown when validation fails.</exception>
        public static ExtensibilityOperationRequest<TConfig, TProperty> Validate<TConfig, TProperty>(
            this ExtensibilityOperationRequest request,
            JsonSchema importConfigSchema,
            Regex resourceTypeRegex,
            JsonSchema resourcePropertySchema)
        {
            var validator = new ExtensibilityRequestValidator(importConfigSchema, resourceTypeRegex, resourcePropertySchema);
            
            validator.ValidateAndThrow(request);

            return new ExtensibilityOperationRequest<TConfig, TProperty>(
                ModelMapper.MapToConcrete<TConfig>(request.Import),
                ModelMapper.MapToConcrete<TProperty>(request.Resource));
        }
    }
}
