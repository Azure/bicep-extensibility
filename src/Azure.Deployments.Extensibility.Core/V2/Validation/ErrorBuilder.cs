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

        public ErrorBuilder WithCode(string code)
        {
            Code = code;
            return this;
        }

        public ErrorBuilder WithMessage(string message)
        {
            Message = message;
            return this;
        }
    }
}
