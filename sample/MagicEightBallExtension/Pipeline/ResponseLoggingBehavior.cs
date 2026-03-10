// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.AspNetCore.Models;
using Azure.Deployments.Extensibility.AspNetCore.Pipeline;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension.Pipeline;

/// <summary>
/// A global pipeline behavior that logs the result of every handler invocation.
/// If the response contains identifiers, they are included in the log record.
/// </summary>
public sealed partial class ResponseLoggingBehavior :
    IResourcePreviewPipelineBehavior,
    IResourceCreateOrUpdatePipelineBehavior,
    IResourceGetPipelineBehavior,
    IResourceDeletePipelineBehavior,
    ILongRunningOperationGetPipelineBehavior
{
    private readonly ILogger<ResponseLoggingBehavior> logger;

    public ResponseLoggingBehavior(ILogger<ResponseLoggingBehavior> logger)
    {
        this.logger = logger;
    }

    async Task<OneOf<ResourcePreview, ErrorResponse>> IResourcePreviewPipelineBehavior.HandleAsync(
        ResourcePreviewSpecification request, ResourcePreviewHandlerDelegate next, CancellationToken cancellationToken)
    {
        var response = await next(request);
        var name = request.Properties["name"]?.GetValue<string>();

        response.Switch(
            preview => this.LogPreviewCompleted(preview.Identifiers["name"]?.GetValue<string>() ?? name, request.Type),
            error => this.LogPreviewFailed(GetErrorLogLevel(error), name, request.Type, error.Error.Message));

        return response;
    }

    async Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdatePipelineBehavior.HandleAsync(
        ResourceSpecification request, ResourceCreateOrUpdateHandlerDelegate next, CancellationToken cancellationToken)
    {
        var response = await next(request);
        var name = request.Properties["name"]?.GetValue<string>();

        response.Switch(
            resource => this.LogCreateOrUpdateCompleted(resource.Identifiers["name"]?.GetValue<string>() ?? name, request.Type),
            lro => this.LogCreateOrUpdateLroStarted(name, request.Type, lro.Status),
            error => this.LogCreateOrUpdateFailed(GetErrorLogLevel(error), name, request.Type, error.Error.Message));

        return response;
    }

    async Task<OneOf<Resource?, ErrorResponse>> IResourceGetPipelineBehavior.HandleAsync(
        ResourceReference request, ResourceGetHandlerDelegate next, CancellationToken cancellationToken)
    {
        var response = await next(request);
        var name = request.Identifiers["name"]?.GetValue<string>();

        response.Switch(
            resource => this.LogGetCompleted(resource?.Identifiers["name"]?.GetValue<string>() ?? name, request.Type, resource is not null),
            error => this.LogGetFailed(GetErrorLogLevel(error), name, request.Type, error.Error.Message));

        return response;
    }

    async Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IResourceDeletePipelineBehavior.HandleAsync(
        ResourceReference request, ResourceDeleteHandlerDelegate next, CancellationToken cancellationToken)
    {
        var response = await next(request);
        var requestName = request.Identifiers["name"]?.GetValue<string>();

        response.Switch(
            resource => this.LogDeleteCompleted(resource?.Identifiers["name"]?.GetValue<string>() ?? requestName, request.Type, resource is not null),
            lro => this.LogDeleteLroStarted(requestName, request.Type, lro.Status),
            error => this.LogDeleteFailed(GetErrorLogLevel(error), requestName, request.Type, error.Error.Message));

        return response;
    }

    async Task<OneOf<LongRunningOperation, ErrorResponse>> ILongRunningOperationGetPipelineBehavior.HandleAsync(
        JsonObject request, LongRunningOperationGetHandlerDelegate next, CancellationToken cancellationToken)
    {
        var response = await next(request);

        response.Switch(
            lro => this.LogLroGetCompleted(lro.Status),
            error => this.LogLroGetFailed(GetErrorLogLevel(error), error.Error.Message));

        return response;
    }

    private static LogLevel GetErrorLogLevel(ErrorResponse error) =>
        error is HttpErrorResponse { StatusCode: >= 500 } ? LogLevel.Error : LogLevel.Information;

    [LoggerMessage(Level = LogLevel.Information, Message = "Preview completed for '{Name}' (type '{Type}').")]
    private partial void LogPreviewCompleted(string? name, string type);


    [LoggerMessage(Message = "Preview failed for '{Name}' (type '{Type}'): {Error}.")]
    private partial void LogPreviewFailed(LogLevel level, string? name, string type, string error);


    [LoggerMessage(Level = LogLevel.Information, Message = "CreateOrUpdate completed for '{Name}' (type '{Type}').")]
    private partial void LogCreateOrUpdateCompleted(string? name, string type);


    [LoggerMessage(Level = LogLevel.Information, Message = "CreateOrUpdate started LRO for '{Name}' (type '{Type}'), status: {Status}.")]
    private partial void LogCreateOrUpdateLroStarted(string? name, string type, string status);


    [LoggerMessage(Message = "CreateOrUpdate failed for '{Name}' (type '{Type}'): {Error}.")]
    private partial void LogCreateOrUpdateFailed(LogLevel level, string? name, string type, string error);


    [LoggerMessage(Level = LogLevel.Information, Message = "Get completed for '{Name}' (type '{Type}'), found: {Found}.")]
    private partial void LogGetCompleted(string? name, string type, bool found);


    [LoggerMessage(Message = "Get failed for '{Name}' (type '{Type}'): {Error}.")]
    private partial void LogGetFailed(LogLevel level, string? name, string type, string error);


    [LoggerMessage(Level = LogLevel.Information, Message = "Delete completed for '{Name}' (type '{Type}'), existed: {Existed}.")]
    private partial void LogDeleteCompleted(string? name, string type, bool existed);


    [LoggerMessage(Level = LogLevel.Information, Message = "Delete started LRO for '{Name}' (type '{Type}'), status: {Status}.")]
    private partial void LogDeleteLroStarted(string? name, string type, string status);


    [LoggerMessage(Message = "Delete failed for '{Name}' (type '{Type}'): {Error}.")]
    private partial void LogDeleteFailed(LogLevel level, string? name, string type, string error);


    [LoggerMessage(Level = LogLevel.Information, Message = "LRO get completed, status: {Status}.")]
    private partial void LogLroGetCompleted(string status);


    [LoggerMessage(Message = "LRO get failed: {Error}.")]
    private partial void LogLroGetFailed(LogLevel level, string error);
}
