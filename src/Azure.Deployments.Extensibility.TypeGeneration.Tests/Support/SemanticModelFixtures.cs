// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;

internal static class SemanticModelFixtures
{
    public static ExtensionTypeSemanticModel CreateContoso(SourceLocation? location = null)
    {
        var text = new StringTypeDefinition { SourceLocation = location };
        var nullableText = new UnionTypeDefinition
        {
            Elements =
            [
                new() { Type = text, SourceLocation = location },
                new() { Type = new NullTypeDefinition { SourceLocation = location }, SourceLocation = location },
            ],
            SourceLocation = location,
        };

        return new ExtensionTypeSemanticModel
        {
            Name = "Contoso",
            Version = "1.2",
            Configuration = new ObjectTypeDefinition
            {
                Name = "ContosoConfiguration",
                Properties =
                [
                    new()
                    {
                        Name = "organization",
                        Description = "Organization",
                        Type = nullableText,
                        SourceLocation = location,
                    },
                ],
                SourceLocation = location,
            },
            ResourceTypes =
            [
                new()
                {
                    Name = "Repository",
                    ApiVersions = ["2026-08-01", "2026-09-01"],
                    ReadableScopes = ResourceScope.All,
                    WritableScopes = ResourceScope.All,
                    Body = new ObjectTypeDefinition
                    {
                        Name = "Repository",
                        Properties =
                        [
                            new()
                            {
                                Name = "name",
                                Type = text,
                                Flags = PropertyFlags.Required | PropertyFlags.Identifier,
                                Description = "Repository name",
                                SourceLocation = location,
                            },
                            new()
                            {
                                Name = "description",
                                Type = nullableText,
                                Description = "Description",
                                SourceLocation = location,
                            },
                            new()
                            {
                                Name = "url",
                                Type = nullableText,
                                Flags = PropertyFlags.ReadOnly,
                                Description = "URL",
                                SourceLocation = location,
                            },
                        ],
                        SourceLocation = location,
                    },
                    ResourceFunctions = [],
                    SourceLocation = location,
                },
            ],
            SourceLocation = location,
        };
    }
}
