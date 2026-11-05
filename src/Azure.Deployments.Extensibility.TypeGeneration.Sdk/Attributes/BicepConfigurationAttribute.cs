// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Attributes;

/// <summary>
/// Marks the extension configuration class for build-time discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BicepConfigurationAttribute : Attribute
{
}
