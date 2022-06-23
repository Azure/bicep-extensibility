// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Extensions;
using Json.Pointer;
using Json.Schema;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core.Validators
{
    public class ExtensibleImportValidator
    {
        private readonly JsonSchema configSchema;

        public ExtensibleImportValidator(JsonSchema configSchema)
        {
            this.configSchema = configSchema;
        }

        public IEnumerable<ExtensibilityError> Validate(ExtensibleImport<JsonElement> import)
        {
            var violations = JsonSchemaValidator.Validate(this.configSchema, import.Config);

            if (violations.Any())
            {
                var baseTarget = import.GetJsonPointer(x => x.Config);

                foreach (var violation in violations)
                {
                    // Prepend "/imports/{import.SymbolicName}" to target.
                    var target = baseTarget.Combine(violation.Target);
                    var error = (violation with { Target = target }).ToExtensibilityError();

                    yield return error;
                }
            }
        }
    }
}
