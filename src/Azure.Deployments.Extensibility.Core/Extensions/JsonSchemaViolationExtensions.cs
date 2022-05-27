using Azure.Deployments.Extensibility.Core.Validators;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    public static class JsonSchemaViolationExtensions
    {
        public static ExtensibilityError ToExtensibilityError(this JsonSchemaViolation violation) =>
            new("JsonSchemaViolation", violation.Target, violation.ErrorMessage);
    }
}
