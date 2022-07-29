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
    public class PreviewSaveResponseExampleProvider : IMultipleExamplesProvider<ExtensibilityOperationResponse>
    {
        public IEnumerable<SwaggerExample<ExtensibilityOperationResponse>> GetExamples()
        {
            var sampleResourcePropertiesForPreviewSave = new Dictionary<string, JsonElement>
            {
                ["propA"] = "valueA".AsJsonElement(),
                ["propB"] = "valueB".AsJsonElement(),
                ["propC"] = "valueC".AsJsonElement(),
            }.AsJsonElement();

            var sampleResourceMetadata = new ExtensibleResourceMetadata(
                new[]
                {
                    JsonPath.Parse("$.properties.propA"),
                    JsonPath.Parse("$.properties.propB"),
                },
                null,
                null);

            yield return SwaggerExample.Create(
                "Success response",
                (ExtensibilityOperationResponse)new ExtensibilityOperationSuccessResponse(
                    new ExtensibleResource<JsonElement>("sampleNamespace/sampleResourceType@v1", sampleResourcePropertiesForPreviewSave),
                    sampleResourceMetadata));

            yield return SwaggerExample.Create(
                "Error response",
                (ExtensibilityOperationResponse)new ExtensibilityOperationErrorResponse(
                    new ExtensibilityError("InvalidProperty", JsonPointer.Parse("/resource/properties/propC"), "Value is invalid.")));
        }
    }
}
