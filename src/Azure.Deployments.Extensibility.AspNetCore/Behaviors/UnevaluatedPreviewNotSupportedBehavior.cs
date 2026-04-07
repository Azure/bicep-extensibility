// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Constants;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// A resource preview behavior that returns an error for a preview request if it contains unevaluated properties or config.
/// </summary>
/// <remarks>This can be used to opt out of previewing resources with unevaluated ARM template expressions.</remarks>
public class UnevaluatedPreviewNotSupportedBehavior : IResourcePreviewBehavior
{
    private const string ConfigPointerSegment = "config";
    private const string PropertiesPointerSegment = "properties";

    public bool UnevaluatedConfigSupported { private get; init; } = false;

    public bool UnevaluatedPropertiesSupported { private get; init; } = false;

    public Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(ResourcePreviewSpecification request, ResourcePreviewHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (!this.UnevaluatedConfigSupported && request.Metadata?.Unevaluated.Any(p => p.Count > 0 && string.Equals(p[0], ConfigPointerSegment, StringComparison.Ordinal)) is true)
        {
            return CreateErrorResponse(new Error(ErrorCodes.PreviewNotSupported, $"Preview is not supported with an unevaluated extension configuration for a resource of type '{GetResourceTypeAndApiVersion(request)}'."));
        }

        if (!this.UnevaluatedPropertiesSupported && request.Metadata?.Unevaluated.Any(p => p.Count > 0 && string.Equals(p[0], PropertiesPointerSegment, StringComparison.Ordinal)) is true)
        {
            return CreateErrorResponse(new Error(ErrorCodes.PreviewNotSupported, $"Preview is not supported with unevaluated properties for a resource of type '{GetResourceTypeAndApiVersion(request)}'."));
        }

        return next(request);
    }

    private static Task<OneOf<ResourcePreview, ErrorResponse>> CreateErrorResponse(Error error) =>
        Task.FromResult<OneOf<ResourcePreview, ErrorResponse>>(new ErrorResponse(error));

    private static string GetResourceTypeAndApiVersion(ResourcePreviewSpecification request) =>
        $"{request.Type}{(!string.IsNullOrEmpty(request.ApiVersion) ? "@" + request.ApiVersion : string.Empty)}";
}
