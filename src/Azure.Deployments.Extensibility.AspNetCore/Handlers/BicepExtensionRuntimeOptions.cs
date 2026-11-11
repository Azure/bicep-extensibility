// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

public sealed class BicepExtensionRuntimeOptions
{
    public List<IComponentRegistrationResolver> GlobalBehaviors { get; } = [];
}
