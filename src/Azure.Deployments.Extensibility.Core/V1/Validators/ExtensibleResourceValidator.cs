// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Extensions;
using Json.More;
using Json.Pointer;
using Json.Schema;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.Validators
{
    /// <summary>
    /// Validates an extensible resource by checking its type against a regex and its properties against a JSON Schema.
    /// </summary>
    public class ExtensibleResourceValidator
    {
        private readonly JsonSchema typeSchema;

        private readonly Func<string, JsonSchema> propertiesSchemaSelector;

        /// <summary>
        /// Initializes a new instance with a resource type regex and a function that resolves
        /// the properties schema from the resource type.
        /// </summary>
        /// <param name="typeRegex">A regex that the resource type must match.</param>
        /// <param name="propertiesSchemaSelector">A function that returns the JSON Schema for the resource properties given its type.</param>
        public ExtensibleResourceValidator(Regex typeRegex, Func<string, JsonSchema> propertiesSchemaSelector)
        {
            this.typeSchema = new JsonSchemaBuilder().Pattern(typeRegex);
            this.propertiesSchemaSelector = propertiesSchemaSelector;
        }

        /// <summary>
        /// Initializes a new instance with a resource type regex and a single JSON Schema for all types.
        /// </summary>
        /// <param name="typeRegex">A regex that the resource type must match.</param>
        /// <param name="propertiesSchema">The JSON Schema for the resource properties.</param>
        public ExtensibleResourceValidator(Regex typeRegex, JsonSchema propertiesSchema)
            : this(typeRegex, _ => propertiesSchema)
        {
        }

        /// <summary>
        /// Validate the resource type and properties.
        /// </summary>
        /// <param name="resource">The resource to validate.</param>
        /// <returns>An enumerable of <see cref="ExtensibilityError"/> instances for each validation failure.</returns>
        public IEnumerable<ExtensibilityError> Validate(ExtensibleResource<JsonElement> resource)
        {
            // Validate resource type.
            var typeErrors = Validate(resource.GetJsonPointer(x => x.Type), this.typeSchema, resource.Type.AsJsonElement());

            if (typeErrors.Any())
            {
                // Stop validating properties if the resource type is invalid.
                return typeErrors;
            }

            // Validate resource properties.
            var propertiesSchema = this.propertiesSchemaSelector(resource.Type);
            var propertiesErrors = Validate(resource.GetJsonPointer(x => x.Properties), propertiesSchema, resource.Properties);

            return propertiesErrors;
        }

        private static IEnumerable<ExtensibilityError> Validate(JsonPointer basePointer, JsonSchema schema, JsonElement value)
        {
            var violations = JsonSchemaValidator.Validate(schema, value);

            if (violations.Any())
            {
                foreach (var violation in violations)
                {
                    // Prepend "/resources/{resource.SymbolicName}" to target.
                    var target = basePointer.Combine(violation.Target);
                    var error = (violation with { Target = target }).ToExtensibilityError();

                    yield return error;
                }
            }
        }
    }
}
