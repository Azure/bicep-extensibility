// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Core.V2.Json;
using FluentAssertions;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Contracts.Models;

public class ResourcePreviewSpecificationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Deserialize_WithMissingMetadata_AcceptsRequest(bool sourceGenerated)
    {
        const string json = """{"type":"Widget","properties":{"name":"example"}}""";

        var result = sourceGenerated
            ? JsonSerializer.Deserialize(json, JsonDefaults.SerializerContext.ResourcePreviewSpecification)
            : JsonSerializer.Deserialize<ResourcePreviewSpecification>(json, JsonDefaults.SerializerOptions);

        result.Should().NotBeNull();
        result!.Type.Should().Be("Widget");
        result.Properties["name"]!.GetValue<string>().Should().Be("example");
    }

    [Fact]
    public void Constructor_WithMissingMetadata_DefaultsToEmptyMetadata()
    {
        var result = new ResourcePreviewSpecification { Type = "Widget", Properties = [] };

        result.Metadata.Should().NotBeNull();
        result.Metadata.Unevaluated.Should().BeEmpty();
    }

    [Theory]
    [InlineData("""{"properties":{}}""")]
    [InlineData("""{"type":"Widget"}""")]
    public void Deserialize_WithMissingRequiredResourceFields_ThrowsJsonException(string json)
    {
        var action = () => JsonSerializer.Deserialize(json, JsonDefaults.SerializerContext.ResourcePreviewSpecification);

        action.Should().Throw<JsonException>();
    }
}
