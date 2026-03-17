// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Extensions;

namespace Azure.Deployments.Extensibility.AspNetCore.Builders;

/// <summary>
/// A builder for configuring the Scalar API explorer, including its title,
/// version examples, and request/response examples injected into the OpenAPI document.
/// </summary>
public class ScalarApiExplorerBuilder
{
    internal string Title { get; private set; } = ScalarExtensions.DefaultTitle;

    internal string[]? ExtensionVersions { get; private set; }

    internal Action<OpenApiExamplesBuilder>? ExamplesConfigurator { get; private set; }

    /// <summary>
    /// Sets the title displayed in the Scalar API explorer UI.
    /// </summary>
    public ScalarApiExplorerBuilder WithTitle(string title)
    {
        Title = title;
        return this;
    }

    /// <summary>
    /// Sets the extension version strings to list as examples in the OpenAPI document
    /// (e.g., the <c>extensionVersion</c> query parameter).
    /// </summary>
    public ScalarApiExplorerBuilder WithExtensionVersions(params string[] versions)
    {
        ExtensionVersions = versions;
        return this;
    }

    /// <summary>
    /// Configures request and response examples to inject into the OpenAPI document.
    /// </summary>
    public ScalarApiExplorerBuilder ConfigureExamples(Action<OpenApiExamplesBuilder> configure)
    {
        ExamplesConfigurator += configure;
        return this;
    }
}
