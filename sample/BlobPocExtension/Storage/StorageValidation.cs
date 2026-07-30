// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;

namespace BlobPocExtension.Storage;

/// <summary>
/// Input validation shared by the storage handlers. The account-name check doubles as
/// an SSRF guard: it bounds what can be interpolated into the account endpoint URL.
/// </summary>
public static partial class StorageValidation
{
    [GeneratedRegex("^[a-z0-9]{3,24}$")]
    private static partial Regex AccountNameRegex();

    /// <summary>
    /// Returns an <see cref="ErrorResponse"/> when the account name is missing or not a valid Azure
    /// Storage account name, otherwise <c>null</c>.
    /// </summary>
    public static ErrorResponse? ValidateAccountName(string? accountName, string target)
    {
        if (string.IsNullOrWhiteSpace(accountName))
        {
            return new ErrorResponse(new Error
            {
                Code = "MissingAccountName",
                Message = "An account name is required.",
                Target = JsonPointer.Parse(target),
            });
        }

        if (AccountNameRegex().IsMatch(accountName))
        {
            return null;
        }

        return new ErrorResponse(new Error
        {
            Code = "InvalidAccountName",
            Message = $"Account name '{accountName}' is invalid. It must match ^[a-z0-9]{{3,24}}$.",
            Target = JsonPointer.Parse(target),
        });
    }
}
