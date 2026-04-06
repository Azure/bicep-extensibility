// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
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
    /// necessary modifications, and outputs a rewritten request and the associated context.
    /// </summary>
    /// <param name="request">The original resource preview request.</param>
    /// <param name="outgoingRequest">When the method returns, contains the rewritten resource preview request with necessary adjustments.</param>
    /// <param name="context">
    /// When the method returns, contains the context created during the rewriting process.
    /// This context can store additional information or track specific properties, aiding in later response processing.
    /// </param>
    /// <returns><c>True</c> if the request was rewritten; otherwise, <c>False</c>.</returns>
    bool RewritePreviewRequest(ResourcePreviewSpecification request, [NotNullWhen(true)] out ResourcePreviewSpecification? outgoingRequest, [NotNullWhen(true)] out ResourcePreviewRewriterContext? context);

    /// <summary>
    /// Rewrites the resource preview response to handle scenarios where Deployments engine sends requests containing unevaluated ARM
    /// template language expressions so that the response can be digested by the Deployments engine. This method processes the response,
    /// typically undoing the rewrites made during request rewriting, and outputs the revised resource preview or an error response.
    /// </summary>
    /// <param name="response">The outgoing response from the resource preview handler.</param>
    /// <param name="context">
    /// The context created during the request rewriting process. This context provides additional information
    /// or tracks specific properties that may influence the response rewriting.
    /// </param>
    /// <example>Restores the original ARM template language expressions in the response, making it digestible to the Deployments engine.</example>
    /// <returns>
    /// A <c>OneOf</c> object containing either the modified <c>ResourcePreview</c> with the necessary updates
    /// or an <c>ErrorResponse</c> indicating the reason why the processing was unsuccessful.
    /// </returns>
    OneOf<ResourcePreview, ErrorResponse> RewritePreviewResponse(ResourcePreview response, ResourcePreviewRewriterContext context);
}

public class ResourcePreviewRewriterContext(ResourcePreviewSpecification originalRequest)
{
    /// <summary>The original preview request.</summary>   
    public ResourcePreviewSpecification OriginalRequest { get; init; } = originalRequest;

    /// <summary>The resource type of the resource being previewed.</summary>
    public string ResourceType => this.OriginalRequest.Type;

    /// <summary>The API version of the resource being previewed.</summary>
    public string? ApiVersion => this.OriginalRequest.ApiVersion;

    /// <summary>Additional properties that can be used to store state during the rewrite operation.</summary>
    public IReadOnlyDictionary<string, object?> Properties => this.additionalProperties;

    private readonly Dictionary<string, object?> additionalProperties = new();

    public T? GetProperty<T>(string name) => this.additionalProperties.TryGetValue(name, out var value) && value is T typed ? typed : default;

    public void SetProperty(string name, object? value) => this.additionalProperties[name] = value;
}
