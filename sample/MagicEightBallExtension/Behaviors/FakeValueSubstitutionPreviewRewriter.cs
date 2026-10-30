// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Json.Pointer;

namespace MagicEightBallExtension.Behaviors;

public class FakeValueSubstitutionPreviewRewriter : FakeValueSubstitutionPreviewRewriterBase
{
    public FakeValueSubstitutionPreviewRewriter() : base(handlesRewritingProperties: true, handlesRewritingConfig: true)
    {
    }

    protected override JsonNode CreateFakeValueForUnevaluatedNode(JsonPointer propertyPointer, JsonNode originalValue)
        => JsonValue.Create("<preview-placeholder>");
}
