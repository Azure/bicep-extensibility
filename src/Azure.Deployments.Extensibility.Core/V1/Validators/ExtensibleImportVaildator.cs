// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Extensions;
using Json.Pointer;
using Json.Schema;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core.Validators
{
    /// <summary>
    /// Validates an extensible import configuration against a JSON Schema.
    /// </summary>
    public class ExtensibleImportValidator
    {
        private readonly JsonSchema configSchema;

        /// <summary>
        /// Initializes a new instance with the specified configuration JSON Schema.
        /// </summary>
        /// <param name="configSchema">The JSON Schema for the import configuration.</param>
        public ExtensibleImportValidator(JsonSchema configSchema)
        {
            this.configSchema = configSchema;
        }

        /// <summary>
        /// Validate the import configuration.
        /// </summary>
        /// <param name="import">The import to validate.</param>
        /// <returns>An enumerable of <see cref="ExtensibilityError"/> instances for each validation failure.</returns>
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
