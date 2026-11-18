// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;

internal static class ContosoSampleFixture
{
    public static string SampleProjectPath => Path.Combine(RealProjectFixture.RepositoryRoot, "sample",
        "Azure.Deployments.Extensibility.TypeGeneration.Sample", "Azure.Deployments.Extensibility.TypeGeneration.Sample.csproj");

    public static string DuplicateConfigurationProjectPath => Path.Combine(RealProjectFixture.RepositoryRoot, "src",
        "Azure.Deployments.Extensibility.TypeGeneration.Tests", "Fixtures", "DuplicateConfiguration", "DuplicateConfiguration.csproj");

    public static (string Path, string Source)[] ReadSources() =>
        Directory.GetFiles(Path.GetDirectoryName(SampleProjectPath)!, "*.cs")
            .Order(StringComparer.Ordinal)
            .Select(path => (Path: Path.GetFileName(path), Source: File.ReadAllText(path)))
            .ToArray();

    public static ExtensionTypeSemanticModel CreateExpected()
    {
        var model = ContosoAuthoringFixture.CreateExpected();
        var resource = model.ResourceTypes[0];
        var body = (ObjectTypeDefinition)resource.Body;
        var nullableText = (UnionTypeDefinition)body.Properties[0].Type;
        var text = nullableText.Elements[0].Type;
        var settings = new UnionTypeDefinition(self =>
        [
            new()
            {
                Type = new ObjectTypeDefinition
                {
                    Name = "RepositorySettings",
                    Properties =
                    [
                        new()
                        {
                            Name = "topics",
                            Description = "Repository topics",
                            Type = new ArrayTypeDefinition { ItemType = text },
                        },
                        new()
                        {
                            Name = "labels",
                            Description = "Custom labels",
                            Type = new ObjectTypeDefinition
                            {
                                Name = "Map:global::Contoso.Models.Repository.settings.labels",
                                Properties = [],
                                AdditionalProperties = nullableText,
                            },
                        },
                        new()
                        {
                            Name = "visibility",
                            Description = "Visibility",
                            Type = new UnionTypeDefinition
                            {
                                Elements =
                                [
                                    new() { Type = new StringLiteralTypeDefinition { Value = "Private" } },
                                    new() { Type = new StringLiteralTypeDefinition { Value = "Public" } },
                                ],
                            },
                        },
                        new() { Name = "parent", Description = "Parent settings", Type = self },
                    ],
                },
            },
            new() { Type = new NullTypeDefinition() },
        ]);
        return model with
        {
            ResourceTypes =
            [
                resource with
                {
                    Body = body with
                    {
                        Properties =
                        [
                            body.Properties[0],
                            body.Properties[1],
                            new() { Name = "settings", Description = "Repository settings", Type = settings },
                            body.Properties[2],
                        ],
                    },
                },
            ],
        };
    }
}
