// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions;

// TODO: Legacy type to be deleted in a future PR once consumers migrate to Hosting.Managed / Hosting.FirstParty.
/// <summary>
/// Internal extension methods for configuring extensibility infrastructure on <see cref="WebApplicationBuilder"/>.
/// </summary>
/// <remarks>Legacy type to be deleted in a future PR.</remarks>
internal static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers extensibility infrastructure services: JSON serialization defaults,
    /// exception handling, and problem details formatting.
    /// </summary>
    public static WebApplicationBuilder AddExtensionInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddBicepExtensionJsonOptions();
        builder.Services.AddBicepExtensionExceptionHandler();
        builder.Services.AddBicepExtensionProblemDetails();
        builder.Services.AddHttpContextAccessor();

        return builder;
    }
}
