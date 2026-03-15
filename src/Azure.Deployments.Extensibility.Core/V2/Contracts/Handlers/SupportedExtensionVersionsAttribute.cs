// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Semver;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Specifies the range of semantic extension versions supported by a handler.
/// When applied to a class that implements <see cref="IHandler"/>, the base class can
/// automatically resolve <see cref="IHandler.SupportedExtensionVersions"/> from this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SupportedExtensionVersionsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SupportedExtensionVersionsAttribute"/> class with the specified range.
    /// The range should use the standard semantic versioning <see href="https://semver-nuget.org/v2.3.x/Semver.html#range-syntax">Range Syntax</see>.
    /// </summary>
    /// <param name="range"></param>
    public SupportedExtensionVersionsAttribute(string range)
    {
        Range = SemVersionRange.Parse(range);
    }

    /// <summary>
    /// Gets the parsed semantic version range.
    /// </summary>
    public SemVersionRange Range { get; }
}
