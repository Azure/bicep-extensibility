// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Utils;
using Json.Pointer;
using Json.Schema;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Validators
{
    public class ResourcePropertiesSchemaValidator(JsonSchema propertiesSchema) : IResourcePropertiesValidator
    {
        private readonly static JsonPointer BaseJsonPointer = JsonPointerBuilder.Build<ResourceRequestBody>(x => x.Properties);

        private readonly JsonSchemaValidator validator = new(propertiesSchema);

        public virtual IReadOnlyList<ErrorDetail> Validate(JsonObject value)
        {
            var schemaViolations = this.validator.Validate(value);

            if (!schemaViolations.Any())
            {
                return Array.Empty<ErrorDetail>();
            }

            return schemaViolations
                .Select(x => new ErrorDetail("InvalidProperty", x.ErrorMessage, BaseJsonPointer.Combine(x.InstanceLocation)))
                .ToList();
        }
    }
}
