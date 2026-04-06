// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when one or more extensibility validation errors occur.
    /// </summary>
    public class ExtensibilityException : Exception
    {
        /// <summary>
        /// Initializes a new instance with the specified errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        public ExtensibilityException(IEnumerable<ExtensibilityError> errors)
        {
            this.Errors = errors;
        }

        /// <summary>
        /// Initializes a new instance with one or more errors.
        /// </summary>
        /// <param name="error">The primary error.</param>
        /// <param name="additionalErrors">Additional errors.</param>
        public ExtensibilityException(ExtensibilityError error, params ExtensibilityError[] additionalErrors)
            : this((new [] { error }).Concat(additionalErrors))
        {
        }

        /// <summary>
        /// Initializes a new instance with a single error defined by its components.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="target">The JSON Pointer to the property causing the error.</param>
        /// <param name="message">The error message.</param>
        public ExtensibilityException(string code, JsonPointer target, string message)
            : this(new ExtensibilityError(code, target, message))
        {
        }

        /// <summary>
        /// Gets the validation errors associated with this exception.
        /// </summary>
        public IEnumerable<ExtensibilityError> Errors { get; }
    }
}
