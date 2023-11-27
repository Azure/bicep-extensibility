// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Utils
{
    public static class ArgumentExceptionHelper
    {
        public static void ThrowIf(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)
        {
            if (condition)
            {
                Throw(expression);
            }
        }

        [DoesNotReturn]
        private static void Throw(string? expression) =>
            throw new ArgumentException($"Expected {expression} to be False, but it was True.");
    }
}
