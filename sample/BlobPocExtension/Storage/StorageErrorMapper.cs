// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace BlobPocExtension.Storage;

/// <summary>
/// Translates <see cref="RequestFailedException"/> into a stable, documented
/// <see cref="ErrorResponse"/>. Well-known statuses get a friendly code/message; everything
/// else surfaces the underlying storage error code and detail so failures are diagnosable.
/// </summary>
public static class StorageErrorMapper
{
    public static ErrorResponse MapStorageError(RequestFailedException exception)
    {
        var (code, message) = exception.Status switch
        {
            403 => ("AuthorizationFailed", "The extension identity is not authorized to perform this storage operation."),
            404 when exception.ErrorCode == "ContainerNotFound" => ("ContainerNotFound", "The specified container does not exist."),
            404 => ("AccountNotFound", "The specified storage account or resource does not exist."),
            _ => ("StorageRequestFailed", BuildDefaultMessage(exception)),
        };

        return new ErrorResponse(new Error
        {
            Code = code,
            Message = message,
            InnerError = BuildInnerError(exception),
        });
    }

    private static string BuildDefaultMessage(RequestFailedException exception)
    {
        var storageErrorCode = string.IsNullOrEmpty(exception.ErrorCode) ? "Unknown" : exception.ErrorCode;
        return $"The storage request failed with status {exception.Status} ({storageErrorCode}): {FirstLine(exception.Message)}";
    }

    private static JsonObject BuildInnerError(RequestFailedException exception)
    {
        var innerError = new JsonObject
        {
            ["status"] = exception.Status,
            ["storageErrorCode"] = string.IsNullOrEmpty(exception.ErrorCode) ? null : exception.ErrorCode,
        };

        if (exception.GetRawResponse() is { } response &&
            response.Headers.TryGetValue("x-ms-request-id", out var requestId))
        {
            innerError["requestId"] = requestId;
        }

        return innerError;
    }

    private static string FirstLine(string message)
    {
        var newlineIndex = message.IndexOf('\n');
        return newlineIndex < 0 ? message : message[..newlineIndex].TrimEnd('\r');
    }
}
