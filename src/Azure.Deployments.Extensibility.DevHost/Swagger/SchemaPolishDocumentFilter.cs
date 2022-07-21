// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.DevHost.Swagger
{
    public class SchemaPolishDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var schemas = swaggerDoc.Components.Schemas;
            var genericRequestSchemaName = typeof(ExtensibilityOperationRequest<JsonElement, JsonElement>).Name;

            schemas[nameof(ExtensibilityOperationRequest)] = schemas[genericRequestSchemaName];
            schemas.Remove(genericRequestSchemaName);

            schemas.Remove(nameof(ExtensibilityOperationResponse));
            schemas[nameof(ExtensibilityOperationSuccessResponse)].AllOf.Clear();
            schemas[nameof(ExtensibilityOperationErrorResponse)].AllOf.Clear();

            schemas.Add("JsonPath", new OpenApiSchema
            {
                Type = "string",
                Format = "json-path",
                Description = @"JSON Path is a query language for JSON documents, which can select parts of JSON structures in the same way as XPath expressions select nodes of XML documents. See ""https://goessner.net/articles/JsonPath"" and ""https://github.com/ietf-wg-jsonpath/draft-ietf-jsonpath-base"" for details.",
                Example = new OpenApiString("$.books[?(@.price<10)]"),
            });

            schemas.Add("JsonPointer", new OpenApiSchema
            {
                Type = "string",
                Format = "json-pointer",
                Description = @"JSON Pointer defines a string syntax for identifying a specific value within a JSON document. See ""https://datatracker.ietf.org/doc/html/rfc6901"" for details.",
                Example = new OpenApiString("/objects/foo/arrays/bar/2"),
            });
        }
    }
}
