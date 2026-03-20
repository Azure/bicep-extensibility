// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    /// <summary>
    /// A criterion that supports overriding the error code and message reported on validation failure.
    /// </summary>
    public interface IConfigurableErrorCriterion
    {
        string ErrorCode { get; set; }

        string ErrorMessage { get; set; }
    }
}
