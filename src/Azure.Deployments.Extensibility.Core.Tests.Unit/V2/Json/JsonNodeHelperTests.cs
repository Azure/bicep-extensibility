// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.V2.Json;
using FluentAssertions;
using Json.Pointer;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Json
{
    public class JsonNodeHelperTests
    {
        [Fact]
        public void RemovePaths_NoOp_ProducesExpectedResults()
        {
            var props = new JsonObject
            {
                ["notRemoved"] = "abc"
            };

            var result1 = JsonNodeHelpers.RemovePaths(props, null, out var mutated1);
            mutated1.Should().BeFalse();
            result1.RemovedObject.Should().BeNull();
            JsonNode.DeepEquals(result1.FilteredObject, props).Should().BeTrue();
            object.ReferenceEquals(result1.FilteredObject, props).Should().BeTrue();

            var result2 = JsonNodeHelpers.RemovePaths(props, [JsonPointer.Parse("#/notThere")], out var mutated2);
            mutated2.Should().BeFalse();
            result2.FilteredObject.Should().BeNull();
            JsonNode.DeepEquals(result2.FilteredObject, props).Should().BeTrue();
        }

        [Fact]
        public void RemovePaths_MixedRemovals_ProducesExceptedResults()
        {
            var props = new JsonObject
            {
                ["notRemoved1"] = "abc",
                ["notRemoved2"] = "[reference('abc').someOutput]",
                ["unevaluated1"] = "[reference('abc').someOutput]",
                ["obj1"] = new JsonObject
                {
                    ["nestedUnevaluated1"] = "[reference('abc').someOutput]"
                },
                ["arr1"] = new JsonArray
                {
                    "[reference('abc').someOutput]"
                },
                ["arr2"] = new JsonArray
                {
                    "first",
                    "[reference('abc').someOutput]",
                    "third"
                }
            };

            var toRemove = new HashSet<JsonPointer>
            {
                JsonPointer.Parse("#/config/kubeconfig"),
                JsonPointer.Parse("#/properties/unevaluated1"),
                JsonPointer.Parse("#/properties/obj1/nestedUnevaluated1"),
                JsonPointer.Parse("#/properties/arr1/0"),
                JsonPointer.Parse("#/properties/arr2/1")
            };

            var filteredResult = JsonNodeHelpers.RemovePaths(props, toRemove, out var mutated, JsonPointer.Parse("#/properties"));

            var expectedFilteredObj = new JsonObject
            {
                ["notRemoved1"] = "abc",
                ["notRemoved2"] = "[reference('abc').someOutput]",
                ["obj1"] = new JsonObject(),
                ["arr1"] = new JsonArray(),
                ["arr2"] = new JsonArray
                {
                    "first",
                    "third"
                }
            };

            var expectedRemovedObj = new JsonObject
            {
                ["unevaluated1"] = "[reference('abc').someOutput]",
                ["obj1"] = new JsonObject
                {
                    ["nestedUnevaluated1"] = "[reference('abc').someOutput]"
                },
                ["arr1"] = new JsonArray
                {
                    "[reference('abc').someOutput]"
                },
                ["arr2"] = new JsonObject
                {
                    ["1"] = "[reference('abc').someOutput]"
                }
            };

            mutated.Should().BeTrue();

            JsonNode.DeepEquals(filteredResult.FilteredObject, expectedFilteredObj).Should().BeTrue();
            object.ReferenceEquals(filteredResult.FilteredObject, props).Should().BeTrue();

            filteredResult.RemovedObject.Should().NotBeNull();
            JsonNode.DeepEquals(filteredResult.RemovedObject, expectedRemovedObj).Should().BeTrue();
        }
    }
}
