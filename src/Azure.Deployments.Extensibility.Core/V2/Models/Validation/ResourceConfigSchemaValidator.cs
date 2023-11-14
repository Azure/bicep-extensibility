// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Utils;
using Json.Pointer;
using Json.Schema;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models.Validation
{
    public class ResourceConfigSchemaValidator : IResourceConfigValidator
    {
        private readonly static JsonPointer BaseJsonPointer = JsonPointerBuilder.Build<ResourceRequestBody>(x => x.Config!);

        private readonly JsonSchemaValidator validator;

        private readonly bool configRequired;

        public ResourceConfigSchemaValidator(JsonSchema configSchema, bool configRequired = true)
        {
            this.validator = new JsonSchemaValidator(configSchema);
            this.configRequired = configRequired;
        }

        public IReadOnlyList<ErrorDetail> Validate(JsonObject? config)
        {
            if (config is null)
            {
                if (this.configRequired)
                {
                    return new[] { new ErrorDetail("InvalidConfig", "Config cannot be null or undefined.", BaseJsonPointer) };
                }

                return Array.Empty<ErrorDetail>();
            }

            var schemaViolations = this.validator.Validate(config);

            return schemaViolations
                .Select(x => new ErrorDetail("InvalidConfig", x.ErrorMessage, BaseJsonPointer.Combine(x.InstanceLocation)))
                .ToList();
        }
    }
}
