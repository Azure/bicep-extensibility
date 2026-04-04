// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    /// <summary>
    /// A criterion that supports overriding the error code and message reported on validation failure.
    /// </summary>
    public interface IConfigurableErrorCriterion
    {
        /// <summary>
        /// Gets or sets the error code reported when this criterion fails.
        /// </summary>
        string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the error message reported when this criterion fails.
        /// </summary>
        string ErrorMessage { get; set; }
    }
}
