// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

public interface IComponentRegistrationResolver
{
    object Resolve(IServiceProvider serviceProvider);
}
