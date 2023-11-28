// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Utils;
using Json.Pointer;
using Json.Schema;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Validators
{
    public class ResourceConfigSchemaValidator(JsonSchema configSchema, bool configRequired = true) : IResourceConfigValidator
    {
        private readonly static JsonPointer BaseJsonPointer = JsonPointerBuilder.Build<ResourceRequestBody>(x => x.Config!);

        private readonly JsonSchemaValidator validator = new(configSchema);

        public virtual IReadOnlyList<ErrorDetail> Validate(JsonObject? config)
        {
            if (config is null)
            {
                if (configRequired)
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
