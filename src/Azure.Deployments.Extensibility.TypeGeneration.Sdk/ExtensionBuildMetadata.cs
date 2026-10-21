// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration;

public sealed record ExtensionBuildMetadata(
    string Name,
    string Version,
    bool IsSingleton = false);
