// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;

internal static class ContosoAuthoringFixture
{
    public static ExtensionBuildMetadata Metadata => new("Contoso", "1.2");

    public static (string Path, string Source)[] Sources =>
    [
        ("Identifiers.cs", """
            using Azure.Bicep.Types.Concrete;
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            namespace Contoso.Models;
            public class RepositoryIdentifiers
            {
                [TypeProperty("Repository name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
                public required string Name { get; set; }
            }
            """),
        ("Repository.cs", """
            using Azure.Bicep.Types.Concrete;
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            namespace Contoso.Models;
            [ResourceType("Repository", ApiVersions = new[] { "2026-08-01", "2026-09-01" })]
            public sealed class Repository : RepositoryIdentifiers
            {
                [TypeProperty("Description")] public string? Description { get; set; }
                [TypeProperty("URL", ObjectTypePropertyFlags.ReadOnly)] public string? Url { get; set; }
            }
            """),
        ("Configuration.cs", """
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            namespace Contoso.Models;
            [BicepConfiguration] public sealed class ContosoConfiguration
            {
                [TypeProperty("Organization")] public string? Organization { get; set; }
            }
            """),
    ];

    public static ExtensionTypeSemanticModel CreateExpected()
    {
        var model = SemanticModelFixtures.CreateContoso();
        var resource = model.ResourceTypes[0];
        var body = (ObjectTypeDefinition)resource.Body;
        // Reflection exports derived declarations before inherited properties.
        return model with
        {
            ResourceTypes =
            [
                resource with
                {
                    Body = body with { Properties = [body.Properties[1], body.Properties[2], body.Properties[0]] },
                },
            ],
        };
    }
}
