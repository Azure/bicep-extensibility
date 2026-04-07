// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// An interface for rewriting resource preview requests and responses to handle scenarios when the Deployments engine sends requests with
/// unevaluated ARM template language expressions.
/// </summary>
public interface IResourcePreviewRewriter
{
    /// <summary>
    /// Rewrites the resource preview request to handle scenarios where Deployments engine sends requests containing unevaluated ARM
    /// template language expressions so that the request can be digested by the extension. This method processes the request, applies
    /// necessary modifications, and returns a rewritten request or the original request if no rewrite is needed.
    /// </summary>
    /// <param name="request">The original resource preview request.</param>
    /// <returns>The rewritten preview request or the original request if no rewrite is needed.</returns>
    ResourcePreviewSpecification RewritePreviewRequest(ResourcePreviewSpecification request);

    /// <summary>
    /// Rewrites the resource preview response to handle scenarios where Deployments engine sends requests containing unevaluated ARM
    /// template language expressions so that the response can be digested by the Deployments engine. This method processes the response,
    /// typically undoing the rewrites made during request rewriting, and outputs the revised resource preview or an error response.
    /// </summary>
    /// <param name="response">The outgoing response from the resource preview handler.</param>
    /// <example>Restores the original ARM template language expressions in the response, making it digestible to the Deployments engine.</example>
    /// <returns>
    /// A <c>OneOf</c> object containing either the modified <c>ResourcePreview</c> with the necessary updates
    /// or an <c>ErrorResponse</c> indicating the reason why the processing was unsuccessful.
    /// </returns>
    OneOf<ResourcePreview, ErrorResponse> RewritePreviewResponse(ResourcePreview response);
}
