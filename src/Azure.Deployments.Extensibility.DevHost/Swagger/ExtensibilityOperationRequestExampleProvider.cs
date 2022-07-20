// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Json.More;
using Swashbuckle.AspNetCore.Filters;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.DevHost.Swagger
{
    public class ExtensibilityOperationRequestExampleProvider : IExamplesProvider<ExtensibilityOperationRequest>
    {
        public ExtensibilityOperationRequest GetExamples()
        {
            var sampleConfig = new Dictionary<string, JsonElement>
            {
                ["configOne"] = "Foo".AsJsonElement(),
                ["configTwo"] = "Bar".AsJsonElement(),
            }.AsJsonElement();

            var sampleProperties = new Dictionary<string, JsonElement>
            {
                ["propA"] = "valueA".AsJsonElement(),
                ["propB"] = "valueB".AsJsonElement(),
                ["propC"] = "valueC".AsJsonElement(),
            }.AsJsonElement();

            var sampleImport = new ExtensibleImport<JsonElement>("SampleProvider", "v1", sampleConfig);
            var sampleResource = new ExtensibleResource<JsonElement>("sampleNamespace/sampleResourceType@v1", sampleProperties);

            return new(sampleImport, sampleResource);
        }
    }
}
