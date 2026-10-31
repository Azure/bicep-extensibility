// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Tool;

[Trait("Category", "Unit")]
public class EtsmTreeWriterTests
{
    [Fact]
    public void Golden_ShowsUnprojectedResourceAndExactMetadata()
    {
        var model = ContosoAuthoringFixture.CreateExpected();
        var output = EtsmTreeWriter.Render(model);
        Assert.Equal(output, EtsmTreeWriter.Render(model));
        Assert.Contains("extension \"Contoso\" version=\"1.2\" singleton=false", output);
        Assert.Contains("resource \"Repository\" versions=[\"2026-08-01\", \"2026-09-01\"]", output);
        Assert.Equal(1, output.Split("resource \"Repository\"").Length - 1);
        Assert.Contains("readableScopes=All writableScopes=All", output);
        Assert.Contains("property \"name\" flags=Required, Identifier description=\"Repository name\"", output);
        Assert.Contains("property \"url\" flags=ReadOnly", output);
        Assert.Contains("configuration:", output);
        Assert.Contains("union", output);
        Assert.Contains("ref #", output);
    }

    [Fact]
    public void Cycles_AndSharedNodes_UseStableReferences()
    {
        var node = new ObjectTypeDefinition("Node", self =>
            ([new() { Name = "next", Type = self }], self));
        var output = EtsmTreeWriter.Render(Model(node));
        Assert.Contains("configuration: #1 object \"Node\"", output);
        Assert.Contains("property \"next\" flags=None description=null: ref #1", output);
        Assert.Contains("additionalProperties: ref #1", output);
        Assert.Equal(output, EtsmTreeWriter.Render(Model(node)));
    }

    [Fact]
    public void ScalarsArraysMapsAndEnums_AreShown()
    {
        var types = new ObjectTypeDefinition
        {
            Name = "Values",
            Properties =
            [
                new() { Name = "enabled", Type = new BooleanTypeDefinition() },
                new() { Name = "count", Type = new IntegerTypeDefinition { MinValue = long.MinValue, MaxValue = long.MaxValue } },
                new() { Name = "secrets", Type = new ArrayTypeDefinition { ItemType = new StringTypeDefinition { Sensitive = true } } },
                new() { Name = "mode", Type = new UnionTypeDefinition { Elements = [new() { Type = new StringLiteralTypeDefinition { Value = "A" } }, new() { Type = new NullTypeDefinition() }] } },
            ],
            AdditionalProperties = new StringTypeDefinition(),
        };
        var output = EtsmTreeWriter.Render(Model(types));
        Assert.Contains("bool", output);
        Assert.Contains("min=-9223372036854775808 max=9223372036854775807", output);
        Assert.Contains("item:", output);
        Assert.Contains("string sensitive=true", output);
        Assert.Contains("literal \"A\"", output);
        Assert.Contains("additionalProperties:", output);
        Assert.Contains(" null", output);
    }

    [Fact]
    public void DeepGraph_DoesNotUseTheCallStack()
    {
        TypeDefinition type = new StringTypeDefinition();
        for (var index = 0; index < 1500; index++)
        {
            type = new ArrayTypeDefinition { ItemType = type };
        }

        Assert.Contains("#1501 string", EtsmTreeWriter.Render(Model(type)));
    }

    [Fact]
    public void UnknownNode_IsAnExplicitInspectionFailure()
    {
        var exception = Assert.Throws<ToolException>(() => EtsmTreeWriter.Render(Model(new UnsupportedType())));
        Assert.Equal("TGH1007", exception.Code);
        Assert.Equal(1, exception.ExitCode);
    }

    [Fact]
    public void Cancellation_IsObserved()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => EtsmTreeWriter.Render(ContosoAuthoringFixture.CreateExpected(), cancellation.Token));
    }

    private static ExtensionTypeSemanticModel Model(TypeDefinition configuration) =>
        new() { Name = "Tree", Version = "1", Configuration = configuration, ResourceTypes = [] };

    private sealed record UnsupportedType : TypeDefinition;
}
