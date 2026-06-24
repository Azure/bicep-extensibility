// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace BlobPocExtension.Storage;

/// <summary>
/// Translates <see cref="RequestFailedException"/> into a stable, documented
/// <see cref="ErrorResponse"/> so the error surface never leaks raw SDK exception text.
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
            _ => ("StorageRequestFailed", $"The storage request failed with status {exception.Status}."),
        };

        return new ErrorResponse(new Error
        {
            Code = code,
            Message = message,
        });
    }
}
