using Azure.ResourceManager.Extensibility.Core.Validators;
using Json.Schema;
using System.Text.RegularExpressions;

namespace Azure.ResourceManager.Extensibility.Core.Extensions
{
    public static class ExtensibilityRequestExtensions
    {
        public static ExtensibilityRequest<TConfig, TProperty> Validate<TConfig, TProperty>(
            this ExtensibilityRequest request,
            JsonSchema importConfigSchema,
            Regex resourceTypeRegex,
            JsonSchema resourcePropertySchema)
        {
            var validator = new ExtensibilityRequestValidator(importConfigSchema, resourceTypeRegex, resourcePropertySchema);
            
            validator.ValidateAndThrow(request);

            return new ExtensibilityRequest<TConfig, TProperty>(
                ModelMapper.MapToConcrete<TConfig>(request.Import),
                ModelMapper.MapToConcrete<TProperty>(request.Resource));
        }
    }
}
