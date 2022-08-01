// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Azure.Deployments.Extensibility.Core.Extensions;
using Json.Schema;

namespace Azure.Deployments.Extensibility.Providers.Graph.Extensions
{
    public static class ExtensibilityOperationRequestExtensions
    {
        public readonly static string GraphToken = "graphToken";
        public readonly static string Name = "name";

        public readonly static JsonSchema ConfigSchema = new JsonSchemaBuilder()
            .Properties((GraphToken, new JsonSchemaBuilder().Type(SchemaValueType.String)))
            .Required(GraphToken)
            .AdditionalProperties(false);
        public readonly static Regex ResourceTypeRegex = new(@"^((?<group>[\w.]+)/)?(?<kind>[\w/]+)@(?<version>.+)$", RegexOptions.Compiled);
        public readonly static JsonSchema ResourcePropertiesSchema = new JsonSchemaBuilder()
            .Properties((Name, new JsonSchemaBuilder().Type(SchemaValueType.String)))
            .Required(Name)
            .AdditionalProperties(true);

        public static ExtensibilityOperationRequest<JsonElement, JsonElement> ProcessAsync(this ExtensibilityOperationRequest request)
        {
            return request.Validate<JsonElement, JsonElement>(
                ConfigSchema,
                ResourceTypeRegex,
                ResourcePropertiesSchema);
        }
    }
}
