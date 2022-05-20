using FluentValidation;
using FluentValidation.Results;
using Json.Schema;
using System.Text.RegularExpressions;

namespace Azure.ResourceManager.Extensibility.Core.Validators
{
    public class ExtensibilityRequestValidator
    {
        private readonly ExtensibleImportValidator importValidator;

        private readonly ExtensibleResourceValidator resourceValidator;

        public ExtensibilityRequestValidator(JsonSchema importConfigSchema, Regex resourceTypeRegex, JsonSchema resourcePropertySchema)
        {
            this.importValidator = new ExtensibleImportValidator(importConfigSchema);
            this.resourceValidator = new ExtensibleResourceValidator(resourceTypeRegex, resourcePropertySchema);
        }

        public void ValidateAndThrow(ExtensibilityRequest request)
        {
            var importValidationResult = this.importValidator.Validate(request.Import);
            var resourceValidationResult = this.resourceValidator.Validate(request.Resource);

            if (!importValidationResult.IsValid || !resourceValidationResult.IsValid)
            {
                var errors = new List<ValidationFailure>();

                errors.AddRange(importValidationResult.Errors);
                errors.AddRange(resourceValidationResult.Errors);
                
                throw new ValidationException(errors);
            }
        }
    }
}
