// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Deployments.Extensibility.Core.Json;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.Json
{
    public class OrdinalIgnoreCaseStringSetConverterTests
    {
        [Fact]
        public void NullSet_SerializesAndDeserializes()
        {
            // Arrange
            var record = new TestRecord { IgnoreCaseSetProperty = null };

            // Act
            var json = JsonSerializer.Serialize(record);
            var deserialized = JsonSerializer.Deserialize<TestRecord>(json);

            // Assert
            Assert.Null(deserialized?.IgnoreCaseSetProperty);
        }

        [Fact]
        public void PopulatedSet_SerializesDeserializesAndIsCaseInsensitive()
        {
            // Arrange
            var record = new TestRecord
            {
                IgnoreCaseSetProperty = new HashSet<string> { "Value1", "Value2", "Value3" }
            };

            // Act
            var json = JsonSerializer.Serialize(record);
            var deserialized = JsonSerializer.Deserialize<TestRecord>(json);

            // Assert
            Assert.NotNull(deserialized?.IgnoreCaseSetProperty);
            Assert.Equal(3, deserialized.IgnoreCaseSetProperty.Count);
            Assert.Contains("Value1", deserialized.IgnoreCaseSetProperty);
            Assert.Contains("value1", deserialized.IgnoreCaseSetProperty);
            Assert.Contains("VALUE2", deserialized.IgnoreCaseSetProperty);
            Assert.Contains("VaLuE3", deserialized.IgnoreCaseSetProperty);
        }

        private record TestRecord
        {
            [JsonConverter(typeof(OrdinalIgnoreCaseStringSetConverter))]
            public ISet<string>? IgnoreCaseSetProperty { get; set; }
        }
    }
}
