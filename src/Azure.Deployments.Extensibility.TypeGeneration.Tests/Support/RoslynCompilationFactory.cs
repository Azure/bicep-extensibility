// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text;
using Azure.Bicep.Types.Concrete;
using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;

internal static class RoslynCompilationFactory
{
    private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

    public static CSharpCompilation Create(string source, string path = "AttributeFixture.cs")
        => Create((path, source));

    public static CSharpCompilation Create(params (string Path, string Source)[] sources)
        => CSharpCompilation.Create(
            "AttributeFixture",
            sources.Select(source => CSharpSyntaxTree.ParseText(
                source.Source,
                new CSharpParseOptions(LanguageVersion.CSharp12),
                path: source.Path,
                encoding: Encoding.UTF8)),
            References,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

    public static PortableExecutableReference EmitReference(Compilation compilation)
    {
        using var image = new MemoryStream();
        var emit = compilation.Emit(image);
        if (!emit.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics));
        }

        return MetadataReference.CreateFromImage(image.ToArray());
    }

    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string assemblyPaths)
        {
            throw new InvalidOperationException("The test host did not provide trusted platform assemblies.");
        }

        var paths = assemblyPaths.Split(Path.PathSeparator)
            .Append(typeof(ResourceTypeAttribute).Assembly.Location)
            .Append(typeof(ObjectTypePropertyFlags).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return ImmutableArray.CreateRange<MetadataReference>(
            paths.Select(path => MetadataReference.CreateFromFile(path)));
    }
}
