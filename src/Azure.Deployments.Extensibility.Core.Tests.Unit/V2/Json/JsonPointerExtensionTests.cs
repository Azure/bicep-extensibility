// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.V2.Json;
using FluentAssertions;
using Json.Pointer;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Json
{
    public class JsonPointerExtensionTests
    {
        [Theory]
        [InlineData("#/properties", "#/properties/abc")]
        [InlineData("#/properties", "#/properties/abc/def")]
        [InlineData("#/properties/abc", "#/properties/abc/def/0")]
        public void IsDescendant_ReturnsTrue(string parent, string descendant) =>
            JsonPointer.Parse(descendant).IsDescendantOf(JsonPointer.Parse(parent)).Should().BeTrue();

        [Theory]
        [InlineData("#/properties", "#/config")]
        [InlineData("#/properties", "#/properties")]
        [InlineData("#/properties", "#/Properties/abc")]
        [InlineData("#/properties/abc", "#/properties/def")]
        [InlineData("#/properties/abc/def", "#/properties/xyz/def/xyz")]
        public void IsDescendant_ReturnsFalse(string parent, string descendant) =>
            JsonPointer.Parse(descendant).IsDescendantOf(JsonPointer.Parse(parent)).Should().BeFalse();

        [Fact]
        public void TryRemove_RemovesExpectedNode()
        {
            var obj = new JsonObject
            {
                ["abc"] = "123",
                ["obj1"] = new JsonObject
                {
                    ["prop1"] = "value1"
                },
                ["arr1"] = new JsonArray
                {
                    "first",
                    "second",
                    "third"
                }
            };

            var expectedObj = (JsonObject)obj.DeepClone();

            JsonPointer.Parse("#/abc").TryRemove(obj, out var abcNode).Should().BeTrue();
            abcNode.Should().NotBeNull();
            ((JsonValue)abcNode!).GetValue<string>().Should().Be("123");
            expectedObj.Remove("abc");
            JsonNode.DeepEquals(expectedObj, obj).Should().BeTrue();

            JsonPointer.Parse("#/obj1/prop1").TryRemove(obj, out var prop1Node).Should().BeTrue();
            prop1Node.Should().NotBeNull();
            ((JsonValue)prop1Node!).GetValue<string>().Should().Be("value1");
            ((JsonObject)expectedObj["obj1"]!).Remove("prop1");
            JsonNode.DeepEquals(expectedObj, obj).Should().BeTrue();

            JsonPointer.Parse("#/arr1/1").TryRemove(obj, out var arr1Index1Node).Should().BeTrue();
            arr1Index1Node.Should().NotBeNull();
            ((JsonValue)arr1Index1Node!).GetValue<string>().Should().Be("second");
            ((JsonArray)expectedObj["arr1"]!).RemoveAt(1);
            JsonNode.DeepEquals(expectedObj, obj).Should().BeTrue();
        }

        [Theory]
        [InlineData("#/xyz")]
        [InlineData("#/ABC")]
        [InlineData("#/123")]
        [InlineData("#/arr1/2")]
        public void TryRemove_ReturnsFalseIfNotFound(string pointer)
        {
            var obj = new JsonObject
            {
                ["abc"] = "123",
                ["arr1"] = new JsonArray
                {
                    "first"
                }
            };

            var expectedObj = obj.DeepClone();

            JsonPointer.Parse(pointer).TryRemove(obj, out var node).Should().BeFalse();
            node.Should().BeNull();
            JsonNode.DeepEquals(expectedObj, obj).Should().BeTrue();
        }
    }
}
