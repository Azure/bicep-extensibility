// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    /// <summary>
    /// Fluent builder for overriding the error code and message of a validation criterion.
    /// </summary>
    public sealed class ErrorBuilder
    {
        internal string? Code { get; private set; }

        internal string? Message { get; private set; }

        /// <summary>
        /// Set the error code to use when validation fails.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <returns>This builder for chaining.</returns>
        public ErrorBuilder WithCode(string code)
        {
            Code = code;
            return this;
        }

        /// <summary>
        /// Set the error message to use when validation fails.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>This builder for chaining.</returns>
        public ErrorBuilder WithMessage(string message)
        {
            Message = message;
            return this;
        }
    }
}
