// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using FluentAssertions;
using Json.Pointer;
using Xunit;

namespace Azure.Deployments.Extensibility.AspNetCore.Tests.Unit.Behaviors;

public class FakeValueSubstitutionPreviewRewriterBaseTests
{
    [Fact]
    public async Task RewritePreview_WithoutUnevaluatedPointers_ReturnsOriginalRequestAndResponse()
    {
        var sut = new TestPreviewRewriter();
        var request = CreateRequest();
        var response = CreateResponse(request);

        (await sut.RewritePreviewRequestAsync(request)).Should().BeSameAs(request);
        sut.RewritePreviewResponse(response).AsT0.Should().BeSameAs(response);
        sut.FakeValueCalls.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, false, "/properties/name")]
    [InlineData(false, false, "/config/name")]
    [InlineData(false, true, "/properties/name")]
    [InlineData(true, false, "/config/name")]
    [InlineData(true, true, "/Properties/name")]
    [InlineData(true, true, "/Config/name")]
    public async Task RewritePreview_WithoutHandledPointers_ReturnsOriginalRequestAndResponse(
        bool handlesRewritingProperties,
        bool handlesRewritingConfig,
        string pointer)
    {
        var sut = new TestPreviewRewriter(handlesRewritingProperties, handlesRewritingConfig);
        var request = CreateRequest(pointer);
        var response = CreateResponse(request);

        (await sut.RewritePreviewRequestAsync(request)).Should().BeSameAs(request);
        sut.RewritePreviewResponse(response).AsT0.Should().BeSameAs(response);
        sut.FakeValueCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task RewritePreview_WithNullConfig_ReturnsOriginalRequestAndResponse()
    {
        var sut = new TestPreviewRewriter();
        var request = CreateRequest("/config/name") with { Config = null };
        var response = CreateResponse(request);

        (await sut.RewritePreviewRequestAsync(request)).Should().BeSameAs(request);
        sut.RewritePreviewResponse(response).AsT0.Should().BeSameAs(response);
        sut.FakeValueCalls.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task RewritePreviewRequest_ReplacesUnevaluatedValuesInHandledScopes(
        bool handlesRewritingProperties,
        bool handlesRewritingConfig)
    {
        var sut = new TestPreviewRewriter(handlesRewritingProperties, handlesRewritingConfig);
        var pointers = new List<string>();

        if (handlesRewritingProperties)
        {
            pointers.Add("/properties/name");
        }

        if (handlesRewritingConfig)
        {
            pointers.Add("/config/name");
        }

        var request = CreateRequest([.. pointers]);
        var originalProperties = request.Properties.DeepClone();
        var originalConfig = request.Config!.DeepClone();

        var result = await sut.RewritePreviewRequestAsync(request);

        result.Should().NotBeSameAs(request);
        result.Properties["name"]!.GetValue<string>()
            .Should()
            .Be(
                handlesRewritingProperties ? "<fake-value>" : "[parameters('name')]");

        result.Config!["name"]!.GetValue<string>()
            .Should()
            .Be(
                handlesRewritingConfig ? "<fake-value>" : "[parameters('configName')]");

        result.Properties["evaluated"]!.GetValue<string>().Should().Be("property-value");
        result.Config["evaluated"]!.GetValue<string>().Should().Be("config-value");
        result.Type.Should().Be(request.Type);
        result.ApiVersion.Should().Be(request.ApiVersion);
        result.ConfigId.Should().BeNull();
        result.Metadata.Should().BeSameAs(request.Metadata);
        sut.FakeValueCalls.Select(call => call.Pointer.ToString()).Should().Equal(pointers);
        JsonNode.DeepEquals(request.Properties, originalProperties).Should().BeTrue();
        JsonNode.DeepEquals(request.Config, originalConfig).Should().BeTrue();

        if (!handlesRewritingProperties)
        {
            result.Properties.Should().BeSameAs(request.Properties);
        }

        if (!handlesRewritingConfig)
        {
            result.Config.Should().BeSameAs(request.Config);
        }
    }

    [Theory]
    [InlineData("properties", "/name")]
    [InlineData("properties", "/nested/value")]
    [InlineData("properties", "/items/0")]
    [InlineData("properties", "/a~1b/~0value")]
    [InlineData("properties", "/nested")]
    [InlineData("properties", "/items")]
    [InlineData("config", "/name")]
    [InlineData("config", "/nested/value")]
    [InlineData("config", "/items/0")]
    [InlineData("config", "/a~1b/~0value")]
    public async Task RewritePreview_ReplacesAndRestoresValuesWithoutMutatingInputs(string scope, string path)
    {
        var pointer = JsonPointer.Parse($"/{scope}{path}");
        var relativePointer = JsonPointer.Parse(path);
        var sut = new TestPreviewRewriter();
        var request = CreateRequest(pointer.ToString());
        var originalProperties = request.Properties.DeepClone();
        var originalConfig = request.Config!.DeepClone();
        relativePointer.TryEvaluate(scope == "properties" ? request.Properties : request.Config, out var originalValue)
            .Should()
            .BeTrue();

        var rewrittenRequest = await sut.RewritePreviewRequestAsync(request);

        relativePointer.TryEvaluate(scope == "properties" ? rewrittenRequest.Properties : rewrittenRequest.Config, out var fakeValue)
            .Should()
            .BeTrue();

        fakeValue!.GetValue<string>().Should().Be("<fake-value>");
        var call = sut.FakeValueCalls.Should().ContainSingle().Subject;
        call.Pointer.ToString().Should().Be(pointer.ToString());
        call.OriginalValue.Should().BeSameAs(originalValue);
        JsonNode.DeepEquals(request.Properties, originalProperties).Should().BeTrue();
        JsonNode.DeepEquals(request.Config, originalConfig).Should().BeTrue();

        var response = CreateResponse(rewrittenRequest);
        var responseProperties = response.Properties.DeepClone();
        var responseConfig = response.Config!.DeepClone();

        var result = sut.RewritePreviewResponse(response);

        result.IsT0.Should().BeTrue();
        var restored = result.AsT0!;
        JsonNode.DeepEquals(restored.Properties, originalProperties).Should().BeTrue();
        JsonNode.DeepEquals(restored.Config, originalConfig).Should().BeTrue();
        JsonNode.DeepEquals(response.Properties, responseProperties).Should().BeTrue();
        JsonNode.DeepEquals(response.Config, responseConfig).Should().BeTrue();
        restored.Type.Should().Be(response.Type);
        restored.ApiVersion.Should().Be(response.ApiVersion);
        restored.ConfigId.Should().Be(response.ConfigId);
        restored.Identifiers.Should().BeSameAs(response.Identifiers);
        restored.Status.Should().Be(response.Status);
        restored.Metadata.Should().BeSameAs(response.Metadata);
    }

    [Fact]
    public async Task RewritePreviewResponse_RestoresMultiplePropertiesAndConfigValues()
    {
        var sut = new TestPreviewRewriter();
        var request = CreateRequest("/properties/name", "/properties/nested/value", "/config/name", "/config/items/0");
        var rewritten = await sut.RewritePreviewRequestAsync(request);
        sut.FakeValueCalls.Should().HaveCount(4);
        var response = CreateResponse(rewritten);

        var result = sut.RewritePreviewResponse(response);

        result.IsT0.Should().BeTrue();
        JsonNode.DeepEquals(result.AsT0!.Properties, request.Properties).Should().BeTrue();
        JsonNode.DeepEquals(result.AsT0.Config, request.Config).Should().BeTrue();
        result.AsT0.Metadata.Unevaluated.Should().BeEquivalentTo(request.Metadata.Unevaluated);
    }

    [Fact]
    public async Task RewritePreview_WithUnrecognizedPointerAlongsideHandledPointer_IgnoresUnrecognizedPointer()
    {
        var sut = new TestPreviewRewriter();
        var request = CreateRequest("/identifiers/name", "/properties/name", "/nonsense");

        var rewritten = await sut.RewritePreviewRequestAsync(request);
        var result = sut.RewritePreviewResponse(CreateResponse(rewritten));

        sut.FakeValueCalls.Should().ContainSingle().Which.Pointer.ToString().Should().Be("/properties/name");
        result.IsT0.Should().BeTrue();
        JsonNode.DeepEquals(result.AsT0!.Properties, request.Properties).Should().BeTrue();
        result.AsT0.Metadata.Unevaluated.Should().BeEquivalentTo(request.Metadata.Unevaluated);
    }

    [Theory]
    [InlineData("properties")]
    [InlineData("config")]
    public async Task RewritePreviewResponse_WhenOutgoingBodyHasChanges_RestoresExpressionsWithoutLosingChanges(string scope)
    {
        var sut = new TestPreviewRewriter();
        var request = CreateRequest($"/{scope}/name", $"/{scope}/nested/value");
        var requestBody = scope == "properties" ? request.Properties : request.Config!;
        var originalBody = requestBody.DeepClone();

        var rewritten = await sut.RewritePreviewRequestAsync(request);
        var rewrittenBody = scope == "properties" ? rewritten.Properties : rewritten.Config!;
        rewrittenBody["name"]!.GetValue<string>().Should().Be("<fake-value>");
        rewrittenBody["nested"].Should().NotBeNull();

        var response = CreateResponse(rewritten);
        var responseBody = scope == "properties" ? response.Properties : response.Config!;
        responseBody["name"] = "changed-name-that-is-not-retained-by-default";
        responseBody["evaluated"] = "updated-by-handler";
        responseBody["id"] = "generated-by-handler";
        responseBody.Remove("nested");
        var outgoingBody = responseBody.DeepClone();

        var result = sut.RewritePreviewResponse(response);

        result.IsT0.Should().BeTrue();
        var restoredBody = scope == "properties" ? result.AsT0!.Properties : result.AsT0!.Config!;
        originalBody["name"]!.GetValue<string>().Should().Be(scope == "properties" ? "[parameters('name')]" : "[parameters('configName')]");
        restoredBody["name"]!.GetValue<string>().Should().Be(originalBody["name"]!.GetValue<string>());
        restoredBody["evaluated"]!.GetValue<string>().Should().Be("updated-by-handler");
        restoredBody["id"]!.GetValue<string>().Should().Be("generated-by-handler");
        restoredBody.ContainsKey("nested").Should().BeFalse();
        JsonNode.DeepEquals(requestBody, originalBody).Should().BeTrue();
        JsonNode.DeepEquals(responseBody, outgoingBody).Should().BeTrue();
    }

    [Theory]
    [InlineData("<changed-value>")]
    [InlineData(null)]
    public async Task RewritePreviewResponse_WithChangedOutgoingValue_RestoresOriginalValue(string? outgoingValue)
    {
        var sut = new TestPreviewRewriter();
        var rewritten = await sut.RewritePreviewRequestAsync(CreateRequest("/properties/name"));
        var response = CreateResponse(rewritten);
        response.Properties["name"] = outgoingValue;

        var result = sut.RewritePreviewResponse(response);

        result.AsT0!.Properties["name"]!.GetValue<string>().Should().Be("[parameters('name')]");
        response.Properties["name"]?.GetValue<string>().Should().Be(outgoingValue);
    }

    [Fact]
    public async Task RewritePreviewResponse_AfterOriginalRequestIsMutated_RestoresSnapshot()
    {
        var sut = new TestPreviewRewriter();
        var request = CreateRequest("/properties/nested/value", "/config/nested/value");
        var originalProperties = request.Properties.DeepClone();
        var originalConfig = request.Config!.DeepClone();
        var rewritten = await sut.RewritePreviewRequestAsync(request);
        request.Properties["nested"]!["value"] = "changed";
        request.Config["nested"]!["value"] = "changed";

        var result = sut.RewritePreviewResponse(CreateResponse(rewritten));

        JsonNode.DeepEquals(result.AsT0!.Properties, originalProperties).Should().BeTrue();
        JsonNode.DeepEquals(result.AsT0.Config, originalConfig).Should().BeTrue();
    }

    [Theory]
    [InlineData("name", "/properties/name")]
    [InlineData("nested", "/properties/nested/value")]
    public async Task RewritePreviewResponse_WhenPropertyOrAncestorWasRemoved_DoesNotRestoreIt(string removedProperty, string pointer)
    {
        var sut = new TestPreviewRewriter();
        var rewritten = await sut.RewritePreviewRequestAsync(CreateRequest(pointer));
        var response = CreateResponse(rewritten);
        response.Properties.Remove(removedProperty);

        var result = sut.RewritePreviewResponse(response);

        result.AsT0.Should().BeSameAs(response);
        result.AsT0!.Properties.ContainsKey(removedProperty).Should().BeFalse();
    }

    [Theory]
    [InlineData("/properties/missing")]
    [InlineData("/properties/nullValue")]
    [InlineData("/properties")]
    [InlineData("/config/missing")]
    [InlineData("/config/nullValue")]
    [InlineData("/config")]
    public async Task RewritePreviewResponse_WithMissingOrNullOriginalValue_ReturnsOriginalResponse(string pointer)
    {
        var sut = new TestPreviewRewriter();
        var rewritten = await sut.RewritePreviewRequestAsync(CreateRequest(pointer));
        var response = CreateResponse(rewritten);

        var result = sut.RewritePreviewResponse(response);

        result.IsT0.Should().BeTrue();
        result.AsT0.Should().BeSameAs(response);
        sut.FakeValueCalls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("<resolved-value>")]
    [InlineData(null)]
    public async Task RewritePreviewResponse_WithCustomFinalValue_RemovesOnlyResolvedPointerFromMetadata(string? outgoingValue)
    {
        var sut = new RetainOutgoingValuePreviewRewriter();
        var request = CreateRequest("/properties/name", "/properties/nested/value");
        var rewritten = await sut.RewritePreviewRequestAsync(request);
        var response = CreateResponse(rewritten);
        response.Properties["name"] = outgoingValue;

        var result = sut.RewritePreviewResponse(response);

        var restored = result.AsT0!;
        restored.Properties["name"]?.GetValue<string>().Should().Be(outgoingValue);
        restored.Properties["nested"]!["value"]!.GetValue<string>().Should().Be("[parameters('nested')]");
        restored.Metadata.Unevaluated!.Value.Should().Equal(JsonPointer.Parse("/properties/nested/value"));
        restored.Metadata.ReadOnly.Should().BeEquivalentTo(response.Metadata.ReadOnly);
        response.Metadata.Unevaluated.Should().BeEquivalentTo(request.Metadata.Unevaluated);
    }

    [Fact]
    public async Task RewritePreviewResponse_WithNullUnevaluatedMetadata_PreservesNullMetadata()
    {
        var sut = new RetainOutgoingValuePreviewRewriter();
        var rewritten = await sut.RewritePreviewRequestAsync(CreateRequest("/properties/name"));
        var response = CreateResponse(rewritten) with { Metadata = new ResourcePreviewMetadata() };

        var result = sut.RewritePreviewResponse(response);

        result.AsT0!.Properties["name"]!.GetValue<string>().Should().Be("<fake-value>");
        result.AsT0.Metadata.Should().BeSameAs(response.Metadata);
        result.AsT0.Metadata.Unevaluated.Should().BeNull();
    }

    [Fact]
    public async Task RewritePreviewResponse_WithFinalizationOverride_PassesRestoredPreviewAndReturnsError()
    {
        var sut = new FinalizingPreviewRewriter();
        var request = CreateRequest("/properties/name");
        var rewritten = await sut.RewritePreviewRequestAsync(request);

        var result = sut.RewritePreviewResponse(CreateResponse(rewritten));

        result.IsT1.Should().BeTrue();
        result.AsT1.Should().BeSameAs(sut.ErrorResponse);
        sut.FinalizedPreview.Should().NotBeNull();
        JsonNode.DeepEquals(sut.FinalizedPreview!.Properties, request.Properties).Should().BeTrue();
    }

    [Fact]
    public async Task RewritePreviewResponse_WhenRewritingIsSkipped_DoesNotFinalize()
    {
        var sut = new FinalizingPreviewRewriter();
        var rewritten = await sut.RewritePreviewRequestAsync(CreateRequest());
        var response = CreateResponse(rewritten);

        sut.RewritePreviewResponse(response).AsT0.Should().BeSameAs(response);
        sut.FinalizedPreview.Should().BeNull();
    }

    private static ResourcePreviewSpecification CreateRequest(params string[] pointers) => new()
    {
        Type = "Widget",
        ApiVersion = "2026-01-01",
        Properties = CreateValues("[parameters('name')]", "property-value"),
        Config = CreateValues("[parameters('configName')]", "config-value"),
        Metadata = new ResourcePreviewSpecificationMetadata
        {
            Unevaluated = [.. pointers.Select(JsonPointer.Parse)]
        }
    };

    private static JsonObject CreateValues(string name, string evaluated) => new()
    {
        ["name"] = name,
        ["evaluated"] = evaluated,
        ["nested"] = new JsonObject { ["value"] = "[parameters('nested')]" },
        ["items"] = new JsonArray("[parameters('item')]", "evaluated-item"),
        ["a/b"] = new JsonObject { ["~value"] = "[parameters('escaped')]" },
        ["nullValue"] = null
    };

    private static ResourcePreview CreateResponse(ResourcePreviewSpecification request) => new()
    {
        Type = request.Type,
        ApiVersion = request.ApiVersion,
        ConfigId = "test-config",
        Properties = (JsonObject)request.Properties.DeepClone(),
        Config = (JsonObject?)request.Config?.DeepClone(),
        Identifiers = new JsonObject { ["name"] = "widget" },
        Status = "Succeeded",
        Metadata = new ResourcePreviewMetadata
        {
            Unevaluated = request.Metadata.Unevaluated,
            ReadOnly = [JsonPointer.Parse("/properties/id")]
        }
    };

    private class TestPreviewRewriter(bool handlesRewritingProperties = true, bool handlesRewritingConfig = true)
        : FakeValueSubstitutionPreviewRewriterBase(handlesRewritingProperties, handlesRewritingConfig)
    {
        public List<(JsonPointer Pointer, JsonNode OriginalValue)> FakeValueCalls { get; } = [];

        protected override JsonNode CreateFakeValueForUnevaluatedNode(JsonPointer propertyPointer, JsonNode originalValue)
        {
            this.FakeValueCalls.Add((propertyPointer, originalValue));

            return JsonValue.Create("<fake-value>");
        }
    }

    private sealed class RetainOutgoingValuePreviewRewriter : TestPreviewRewriter
    {
        protected override JsonNode? DecideFinalValueForUnevaluatedNode(JsonPointer propertyPointer, JsonNode originalValue, JsonNode? outgoingValue) =>
            propertyPointer == JsonPointer.Parse("/properties/name")
                ? outgoingValue
                : base.DecideFinalValueForUnevaluatedNode(propertyPointer, originalValue, outgoingValue);
    }

    private sealed class FinalizingPreviewRewriter : TestPreviewRewriter
    {
        public ResourcePreview? FinalizedPreview { get; private set; }

        public ErrorResponse ErrorResponse { get; } = new()
        {
            Error = new Error { Code = "PreviewRejected", Message = "The preview was rejected." }
        };

        protected override OneOf<ResourcePreview, ErrorResponse> Finalize(ResourcePreview preview)
        {
            this.FinalizedPreview = preview;

            return this.ErrorResponse;
        }
    }
}
