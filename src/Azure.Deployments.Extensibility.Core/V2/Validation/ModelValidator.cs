// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public class ModelValidator<TModel>
        where TModel : class
    {
        private readonly List<IModelValidationRule<TModel>> rules = [];

        public ModelValidator<TModel> AddRule<TRule>(Action<TRule>? configureRule = null)
             where TRule : IModelValidationRule<TModel>, new()
        {
            var rule = new TRule();

            configureRule?.Invoke(rule);
            this.rules.Add(rule);

            return this;
        }

        public Error? Validate(TModel model)
        {
            var errorDetails = new List<ErrorDetail>();

            foreach (var rule in this.rules)
            {
                var errorDetailCount = errorDetails.Count;

                errorDetails.AddRange(rule.Validate(model));

                if (errorDetails.Count > errorDetailCount && rule.BailOnError)
                {
                    return AggregateErrorDetails(errorDetails);
                }
            }

            return AggregateErrorDetails(errorDetails);
        }

        private static Error? AggregateErrorDetails(List<ErrorDetail> errorDetails) => errorDetails.Count switch
        {
            0 => null,
            1 => errorDetails[0].ToError(),
            _ => new Error
            {
                Code = "MultipleErrorsOccurred",
                Message = "Multiple errors occurred. Please refer to details for more information.",
                Details = errorDetails,
            },
        };
    }
}
