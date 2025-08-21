// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.Exceptions
{
    public class ExtensibilityException : Exception
    {
        public ExtensibilityException(IEnumerable<ExtensibilityError> errors)
        {
            this.Errors = errors;
        }

        public ExtensibilityException(ExtensibilityError error, params ExtensibilityError[] additionalErrors)
            : this((new [] { error }).Concat(additionalErrors))
        {
        }

        public ExtensibilityException(string code, JsonPointer target, string message)
            : this(new ExtensibilityError(code, target, message))
        {
        }

        public IEnumerable<ExtensibilityError> Errors { get; }

        public override string Message
        {
            get
            {
                var firstError = this.Errors.FirstOrDefault();

                return firstError is not null ? $"{firstError.Code}: {firstError.Message}" : string.Empty;
            }
        }
    }
}
