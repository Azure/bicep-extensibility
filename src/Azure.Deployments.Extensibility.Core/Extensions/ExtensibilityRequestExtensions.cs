// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Validators;
using Json.Schema;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    public static class ExtensibilityRequestExtensions
    {
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
