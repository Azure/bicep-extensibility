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

            var result2 = JsonNodeHelpers.RemovePaths(props, new HashSet<JsonPointer>([JsonPointer.Parse("#/notThere")]), out var mutated2);
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
                    ["unevaluated2"] = "[reference('abc').someOutput]"
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
                },
                ["obj2"] = new JsonObject
                {
                    ["arr3"] = new JsonArray
                    {
                        new JsonArray
                        {
                            new JsonObject
                            {
                                ["asdf"] = "asdfghjkl"
                            }
                        },
                        new JsonArray
                        {
                            new JsonObject
                            {
                                ["asdf"] = "qwerty",
                                ["unevaluated3"] = "[reference('abc').someOutput]"
                            }
                        }
                    }
                }
            };

            var toRemove = new HashSet<JsonPointer>
            {
                JsonPointer.Parse("#/config/kubeconfig"),
                JsonPointer.Parse("#/properties/unevaluated1"),
                JsonPointer.Parse("#/properties/obj1/unevaluated2"),
                JsonPointer.Parse("#/properties/arr1/0"),
                JsonPointer.Parse("#/properties/arr2/1"),
                JsonPointer.Parse("#/properties/obj2/arr3/1/0/unevaluated3")
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
                },
                ["obj2"] = new JsonObject
                {
                    ["arr3"] = new JsonArray
                    {
                        new JsonArray
                        {
                            new JsonObject
                            {
                                ["asdf"] = "asdfghjkl"
                            }
                        },
                        new JsonArray
                        {
                            new JsonObject
                            {
                                ["asdf"] = "qwerty"
                            }
                        }
                    }
                }
            };

            var expectedRemovedObj = new JsonObject
            {
                ["unevaluated1"] = "[reference('abc').someOutput]",
                ["obj1"] = new JsonObject
                {
                    ["unevaluated2"] = "[reference('abc').someOutput]"
                },
                ["arr1"] = new JsonObject
                {
                    ["0"] = "[reference('abc').someOutput]"
                },
                ["arr2"] = new JsonObject
                {
                    ["1"] = "[reference('abc').someOutput]"
                },
                ["obj2"] = new JsonObject
                {
                    ["arr3"] = new JsonObject
                    {
                        ["1"] = new JsonObject
                        {
                            ["0"] = new JsonObject
                            {
                                ["unevaluated3"] = "[reference('abc').someOutput]"
                            }
                        }
                    }
                }
            };

            mutated.Should().BeTrue();

            filteredResult.FilteredObject.Should().DeepEqual(expectedFilteredObj);
            object.ReferenceEquals(filteredResult.FilteredObject, props).Should().BeTrue();

            filteredResult.RemovedObject.Should().NotBeNull();
            filteredResult.RemovedObject.Should().DeepEqual(expectedRemovedObj);

            filteredResult.ArrayPaths.Should().NotBeNullOrEmpty();
            filteredResult.ArrayPaths.Should().ContainSingle(p => p == JsonPointer.Parse("#/arr1"));
            filteredResult.ArrayPaths.Should().ContainSingle(p => p == JsonPointer.Parse("#/arr2"));
            filteredResult.ArrayPaths.Should().ContainSingle(p => p == JsonPointer.Parse("#/obj2/arr3"));
            filteredResult.ArrayPaths.Should().ContainSingle(p => p == JsonPointer.Parse("#/obj2/arr3/1"));
            filteredResult.ArrayPaths.Should().HaveCount(4);
        }
    }
}
