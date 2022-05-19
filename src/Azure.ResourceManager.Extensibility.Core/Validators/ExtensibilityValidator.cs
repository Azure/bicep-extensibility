using FluentValidation;
using FluentValidation.Results;

namespace Azure.ResourceManager.Extensibility.Core.Validators
{
    public abstract class ExtensibilityValidator<T> : AbstractValidator<T>
    {
        public override ValidationResult Validate(ValidationContext<T> context)
        {
            var result = base.Validate(context);

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    // PropertyName is usually in the format of "<Prop1>.<Prop2>", but if the error is
                    // from JsonSchemaValidator, PropertyName is already in the format of a JsonPointer.
                    error.PropertyName = PropertyChainConverter.ConvertToJsonPointer(error.PropertyName).ToString();
                }
            }

            return result;
        }
    }
}
