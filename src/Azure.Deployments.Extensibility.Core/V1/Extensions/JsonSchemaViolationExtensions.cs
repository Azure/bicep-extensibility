// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Validators;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="JsonSchemaViolation"/>.
    /// </summary>
    public static class JsonSchemaViolationExtensions
    {
        /// <summary>
        /// Convert a <see cref="JsonSchemaViolation"/> to an <see cref="ExtensibilityError"/> with code "JsonSchemaViolation".
        /// </summary>
        public static ExtensibilityError ToExtensibilityError(this JsonSchemaViolation violation) =>
            new("JsonSchemaViolation", violation.Target, violation.ErrorMessage);
    }
}
