// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

internal sealed class BicepExtensionRuntimeOptions
{
    internal List<ComponentRegistration> GlobalBehaviors { get; } = [];
}
