// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MagicEightBallExtension.Decorators.V1;

/// <summary>
/// Validates that the resource API version is accepted for v1 handlers.
/// </summary>
public sealed class ApiVersionValidationBehavior : ApiVersionValidationBehaviorBase
{
    public ApiVersionValidationBehavior()
        : base("2024-01-01", "2024-01-01-preview")
    {
    }
}
