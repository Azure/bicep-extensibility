// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>Adapts a preview request/response rewriter to a resource preview behavior.</summary>
public class PreviewRewriterBehavior(IResourcePreviewRewriter previewRewriter) : IResourcePreviewBehavior
{
    private IResourcePreviewRewriter PreviewRewriter { get; } = previewRewriter;

    public async Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
        ResourcePreviewSpecification request,
        ResourcePreviewHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        var wasRewritten = this.PreviewRewriter.RewritePreviewRequest(request, out var rewrittenRequest, out var context);

        var response = await next(rewrittenRequest ?? request);

        return wasRewritten && response.IsT0
            ? this.PreviewRewriter.RewritePreviewResponse(response.AsT0!, context!)
            : response;
    }
}
