using Azure.ResourceManager.Extensibility.Core.Exceptions;
using Json.Schema;
using System.Text.RegularExpressions;

namespace Azure.ResourceManager.Extensibility.Core.Validators
{
    public class ExtensibilityRequestValidator
    {
        private readonly JsonSchema importConfigSchema;

        private readonly Regex resourceTypeRegex;

        private readonly JsonSchema resourcePropertySchema;

        public ExtensibilityRequestValidator(JsonSchema importConfigSchema, Regex resourceTypeRegex, JsonSchema resourcePropertySchema)
        {
            this.importConfigSchema = importConfigSchema;
            this.resourceTypeRegex = resourceTypeRegex;
            this.resourcePropertySchema = resourcePropertySchema;
        }

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
