// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>Adapts a preview request/response rewriter to a resource preview behavior.</summary>
public class PreviewRewriteBehavior(IResourcePreviewRewriter previewRewriter) : IResourcePreviewBehavior
{
    private IResourcePreviewRewriter PreviewRewriter { get; } = previewRewriter;

    public async Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
        ResourcePreviewSpecification request,
        ResourcePreviewHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        var processedRequest = this.PreviewRewriter.RewritePreviewRequest(request);
        var wasRewritten = !ReferenceEquals(request, processedRequest);

        var response = await next(processedRequest);

        return wasRewritten && response.IsT0 ? this.PreviewRewriter.RewritePreviewResponse(response.AsT0!, request) : response;
    }
}
