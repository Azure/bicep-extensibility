// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.Deployments.Extensibility.TypeGeneration.Attributes;

namespace Contoso.Models;

public sealed class RepositorySettings
{
    [TypeProperty("Repository topics")]
    public List<string> Topics { get; set; } = [];

    [TypeProperty("Custom labels")]
    public Dictionary<string, string?> Labels { get; set; } = [];

    [TypeProperty("Visibility")]
    public RepositoryVisibility Visibility { get; set; }

    [TypeProperty("Parent settings")]
    public RepositorySettings? Parent { get; set; }
}

public enum RepositoryVisibility
{
    Private,
    Public,
}
