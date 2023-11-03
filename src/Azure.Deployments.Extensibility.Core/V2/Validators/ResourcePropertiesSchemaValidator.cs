// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using Json.Schema;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public class ResourcePropertiesSchemaValidator : IValidator<JsonObject, IReadOnlyList<ErrorDetail>>
    {
        private readonly static JsonPointer BaseJsonPointer = JsonPointer.Create("properties");

        private readonly JsonSchemaValidator validator;

        public ResourcePropertiesSchemaValidator(JsonSchema propertiesSchema)
        {
            this.validator = new JsonSchemaValidator(propertiesSchema);
        }

        public IReadOnlyList<ErrorDetail> Validate(JsonObject value)
        {
            var schemaViolations = this.validator.Validate(value);

            if (!schemaViolations.Any())
            {
                return Array.Empty<ErrorDetail>();
            }

            return schemaViolations
                .Select(x => new ErrorDetail("InvalidProperty", x.ErrorMessage, BaseJsonPointer.Combine(x.Target)))
                .ToList();
        }
    }
}
