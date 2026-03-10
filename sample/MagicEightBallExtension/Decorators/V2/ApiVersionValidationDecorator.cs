// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MagicEightBallExtension.Decorators.V2;

/// <summary>
/// Validates that the resource API version is accepted for v2 handlers.
/// </summary>
public sealed class ApiVersionValidationDecorator : ApiVersionValidationDecoratorBase
{
    public ApiVersionValidationDecorator()
        : base("2025-01-01", "2025-01-01-preview")
    {
    }
}
