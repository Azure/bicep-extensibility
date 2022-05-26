using Azure.ResourceManager.Extensibility.Core.Validators;

namespace Azure.ResourceManager.Extensibility.Core.Extensions
{
    public static class JsonSchemaViolationExtensions
    {
        public static ExtensibilityError ToExtensibilityError(this JsonSchemaViolation violation) =>
            new("JsonSchemaViolation", violation.Target, violation.ErrorMessage);
    }
}
