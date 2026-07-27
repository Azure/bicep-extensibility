// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

internal static class HandlerContractTypes
{
    private static readonly Type[] SupportedBehaviorInterfaces =
    [
        typeof(IHandlerBehavior<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>),
        typeof(IHandlerBehavior<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>),
        typeof(IHandlerBehavior<ResourceReference, OneOf<Resource?, ErrorResponse>>),
        typeof(IHandlerBehavior<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>>),
        typeof(IHandlerBehavior<JsonObject, OneOf<LongRunningOperation, ErrorResponse>>),
    ];

    internal static bool IsSupportedBehavior(Type behaviorType) =>
        SupportedBehaviorInterfaces.Any(@interface => @interface.IsAssignableFrom(behaviorType));
}
