// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Json.More;
using Json.Path;
using Json.Pointer;
using Swashbuckle.AspNetCore.Filters;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.DevHost.Swagger
{
    public class ExtensibilityOperationResponseExampleProvider : IMultipleExamplesProvider<ExtensibilityOperationResponse>
    {
        public IEnumerable<SwaggerExample<ExtensibilityOperationResponse>> GetExamples()
        {
            var sampleResourceProperties = new Dictionary<string, JsonElement>
            {
                ["propA"] = "valueA".AsJsonElement(),
                ["propB"] = "valueB".AsJsonElement(),
                ["propC"] = "valueC".AsJsonElement(),
            }.AsJsonElement();

            yield return SwaggerExample.Create(
                "Success response",
                (ExtensibilityOperationResponse)new ExtensibilityOperationSuccessResponse(
                    new ExtensibleResource<JsonElement>("sampleNamespace/sampleResourceType@v1", sampleResourceProperties)));

            yield return SwaggerExample.Create(
                "Error response",
                (ExtensibilityOperationResponse)new ExtensibilityOperationErrorResponse(
                    new ExtensibilityError("InvalidProviderConfig", JsonPointer.Parse("/import/config/foo"), "Configuration value is invalid.")));
        }
    }
}
