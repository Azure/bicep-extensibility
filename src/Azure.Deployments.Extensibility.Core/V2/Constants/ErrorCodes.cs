// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Constants;

public static class ErrorCodes
{
    /// <summary>Indicates the preview operation is not supported. The Deployments engine will ignore this error during Deployment
    /// preflight and what-if.</summary>
    public const string PreviewNotSupported = nameof(PreviewNotSupported);
}
