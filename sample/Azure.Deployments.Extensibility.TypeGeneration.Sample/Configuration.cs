// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Attributes;

namespace Contoso.Models;

[BicepConfiguration]
public sealed class ContosoConfiguration
{
    [TypeProperty("Organization")]
    public string? Organization { get; set; }
}
