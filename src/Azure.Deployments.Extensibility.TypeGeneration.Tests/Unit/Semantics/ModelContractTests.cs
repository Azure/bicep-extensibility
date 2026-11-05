// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Semantics;

[Trait("Category", "Unit")]
public class ModelContractTests
{
    private static readonly IReadOnlyDictionary<Type, Member[]> Contracts = new Dictionary<Type, Member[]>
    {
        [typeof(ExtensionTypeSemanticModel)] =
        [
            new("Name", typeof(string), true), new("Version", typeof(string), true),
            new("IsSingleton", typeof(bool)), new("IsPreview", typeof(bool)), new("IsDeprecated", typeof(bool)),
            new("Configuration", typeof(TypeDefinition)), new("FallbackResourceType", typeof(ResourceTypeDefinition)),
            new("ResourceTypes", typeof(ImmutableArray<ResourceTypeDefinition>), true), new("SourceLocation", typeof(SourceLocation)),
        ],
        [typeof(ResourceTypeDefinition)] =
        [
            new("Name", typeof(string), true), new("ApiVersions", typeof(ImmutableArray<string>), true),
            new("ReadableScopes", typeof(ResourceScope), true), new("WritableScopes", typeof(ResourceScope), true),
            new("Body", typeof(TypeDefinition), true),
            new("ResourceFunctions", typeof(ImmutableArray<ResourceFunctionDefinition>), true), new("SourceLocation", typeof(SourceLocation)),
        ],
        [typeof(ProjectedResourceTypeDefinition)] =
        [
            new("Name", typeof(string), true), new("ApiVersion", typeof(string)),
            new("ReadableScopes", typeof(ResourceScope), true), new("WritableScopes", typeof(ResourceScope), true),
            new("Body", typeof(TypeDefinition), true),
            new("ResourceFunctions", typeof(ImmutableArray<ProjectedResourceFunctionDefinition>), true), new("SourceLocation", typeof(SourceLocation)),
        ],
        [typeof(TypeDefinition)] = [new("SourceLocation", typeof(SourceLocation))],
        [typeof(AnyTypeDefinition)] = [],
        [typeof(NullTypeDefinition)] = [],
        [typeof(BooleanTypeDefinition)] = [],
        [typeof(IntegerTypeDefinition)] = [new("MinValue", typeof(long?)), new("MaxValue", typeof(long?))],
        [typeof(StringTypeDefinition)] =
        [
            new("Sensitive", typeof(bool)), new("MinLength", typeof(long?)),
            new("MaxLength", typeof(long?)), new("Pattern", typeof(string)),
        ],
        [typeof(StringLiteralTypeDefinition)] = [new("Value", typeof(string), true)],
        [typeof(ArrayTypeDefinition)] =
        [
            new("ItemType", typeof(TypeDefinition), true), new("MinLength", typeof(long?)), new("MaxLength", typeof(long?)),
        ],
        [typeof(ObjectTypeDefinition)] =
        [
            new("Name", typeof(string), true), new("Properties", typeof(ImmutableArray<PropertyDefinition>), true),
            new("AdditionalProperties", typeof(TypeDefinition)), new("Sensitive", typeof(bool)),
        ],
        [typeof(DiscriminatedObjectTypeDefinition)] =
        [
            new("Name", typeof(string), true), new("Discriminator", typeof(string), true),
            new("BaseProperties", typeof(ImmutableArray<PropertyDefinition>), true),
            new("Cases", typeof(ImmutableDictionary<string, ObjectTypeDefinition>), true),
        ],
        [typeof(UnionTypeDefinition)] = [new("Elements", typeof(ImmutableArray<UnionElementDefinition>), true)],
        [typeof(UnionElementDefinition)] =
        [
            new("Type", typeof(TypeDefinition), true), new("Availability", typeof(VersionAvailability)),
            new("SourceLocation", typeof(SourceLocation)),
        ],
        [typeof(PropertyDefinition)] =
        [
            new("Name", typeof(string), true), new("Type", typeof(TypeDefinition), true), new("Flags", typeof(PropertyFlags)),
            new("Description", typeof(string)), new("Versioning", typeof(PropertyVersioning)), new("SourceLocation", typeof(SourceLocation)),
        ],
        [typeof(VersionAvailability)] = [new("AddedAt", typeof(string)), new("RemovedAt", typeof(string))],
        [typeof(VersionTransition<>)] =
        [
            new("AtVersion", typeof(string), true), new("PreviousValue", typeof(VersionTransition<>).GetGenericArguments()[0], true),
        ],
        [typeof(PropertyVersioning)] =
        [
            new("Availability", typeof(VersionAvailability)), new("NameChangedFrom", typeof(VersionTransition<string>)),
            new("TypeChangedFrom", typeof(VersionTransition<TypeDefinition>)), new("RequiredChangedFrom", typeof(VersionTransition<bool>)),
        ],
        [typeof(ResourceFunctionDefinition)] =
        [
            new("Name", typeof(string), true), new("InputType", typeof(TypeDefinition)), new("OutputType", typeof(TypeDefinition), true),
            new("Description", typeof(string)), new("Availability", typeof(VersionAvailability)), new("SourceLocation", typeof(SourceLocation)),
        ],
        [typeof(ProjectedResourceFunctionDefinition)] =
        [
            new("Name", typeof(string), true), new("InputType", typeof(TypeDefinition)), new("OutputType", typeof(TypeDefinition), true),
            new("Description", typeof(string)), new("SourceLocation", typeof(SourceLocation)),
        ],
        [typeof(SourceLocation)] =
        [
            new("FilePath", typeof(string), true), new("Start", typeof(int), true), new("Length", typeof(int), true),
        ],
    };

    public static IEnumerable<object[]> SemanticRecordTypes()
        => Contracts.Keys.Select(type => new object[] { type });

    [Fact]
    public void PublicModelInventory_MatchesTheSpecification()
    {
        var expected = Contracts.Keys.Append(typeof(PropertyFlags)).Append(typeof(ResourceScope));
        var actual = typeof(TypeDefinition).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == typeof(TypeDefinition).Namespace);

        Assert.Equal(expected.Select(type => type.Name).Order(StringComparer.Ordinal),
            actual.Select(type => type.Name).Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(SemanticRecordTypes))]
    public void ModelProperties_MatchTheSignedNamesTypesAndInitializationRules(Type modelType)
    {
        var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var expected = Contracts[modelType];
        Assert.Equal(expected.Select(member => member.Name).Order(StringComparer.Ordinal),
            properties.Select(property => property.Name).Order(StringComparer.Ordinal));
        Assert.Empty(modelType.GetFields(BindingFlags.Public | BindingFlags.Instance));

        foreach (var member in expected)
        {
            var property = Assert.Single(properties, property => property.Name == member.Name);
            Assert.Equal(member.Type, property.PropertyType);
            Assert.Equal(member.Required, property.IsDefined(typeof(RequiredMemberAttribute)));
            Assert.True(property.GetMethod?.IsPublic);
            Assert.True(property.SetMethod?.IsPublic);
            Assert.Contains(typeof(IsExternalInit), property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers());
            AssertSemanticFieldType(property.PropertyType);
        }

        if (modelType == typeof(TypeDefinition))
        {
            Assert.True(modelType.IsAbstract);
        }
        else
        {
            Assert.True(modelType.IsSealed);
            Assert.Empty(Assert.Single(modelType.GetConstructors()).GetParameters());
        }
    }

    [Theory]
    [InlineData(PropertyFlags.None, 0)]
    [InlineData(PropertyFlags.Required, 1)]
    [InlineData(PropertyFlags.ReadOnly, 2)]
    [InlineData(PropertyFlags.WriteOnly, 4)]
    [InlineData(PropertyFlags.DeployTimeConstant, 8)]
    [InlineData(PropertyFlags.Identifier, 16)]
    public void PropertyFlags_MatchTheSemanticBitSet(PropertyFlags flag, int value)
    {
        Assert.Equal(value, (int)flag);
        Assert.True(typeof(PropertyFlags).IsDefined(typeof(FlagsAttribute)));
        Assert.Equal(typeof(TypeDefinition).Assembly, typeof(PropertyFlags).Assembly);
    }

    [Theory]
    [InlineData(ResourceScope.None, 0)]
    [InlineData(ResourceScope.Tenant, 1)]
    [InlineData(ResourceScope.ManagementGroup, 2)]
    [InlineData(ResourceScope.Subscription, 4)]
    [InlineData(ResourceScope.ResourceGroup, 8)]
    [InlineData(ResourceScope.Extension, 16)]
    [InlineData(ResourceScope.AllExceptExtension, 15)]
    [InlineData(ResourceScope.All, 31)]
    public void ResourceScopes_MatchTheSemanticBitSet(ResourceScope scope, int value)
    {
        Assert.Equal(value, (int)scope);
        Assert.True(typeof(ResourceScope).IsDefined(typeof(FlagsAttribute)));
    }

    [Theory]
    [InlineData(typeof(ExtensionTypeSemanticModel), "Configuration", NullabilityState.Nullable)]
    [InlineData(typeof(ExtensionTypeSemanticModel), "Name", NullabilityState.NotNull)]
    [InlineData(typeof(ResourceTypeDefinition), "Body", NullabilityState.NotNull)]
    [InlineData(typeof(ProjectedResourceTypeDefinition), "ApiVersion", NullabilityState.Nullable)]
    [InlineData(typeof(ResourceFunctionDefinition), "InputType", NullabilityState.Nullable)]
    [InlineData(typeof(ResourceFunctionDefinition), "OutputType", NullabilityState.NotNull)]
    [InlineData(typeof(PropertyDefinition), "Versioning", NullabilityState.NotNull)]
    [InlineData(typeof(PropertyVersioning), "TypeChangedFrom", NullabilityState.Nullable)]
    [InlineData(typeof(TypeDefinition), "SourceLocation", NullabilityState.Nullable)]
    public void PublicNullability_IsExplicit(Type modelType, string propertyName, NullabilityState expected)
    {
        var property = modelType.GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Missing property '{modelType.Name}.{propertyName}'.");

        Assert.Equal(expected, new NullabilityInfoContext().Create(property).ReadState);
    }

    [Theory]
    [InlineData("ExtensionTypeSemanticModel", "Name")]
    [InlineData("ExtensionTypeSemanticModel", "Version")]
    [InlineData("ExtensionTypeSemanticModel", "ResourceTypes")]
    [InlineData("ResourceTypeDefinition", "Body")]
    [InlineData("PropertyDefinition", "Type")]
    [InlineData("ArrayTypeDefinition", "ItemType")]
    [InlineData("UnionTypeDefinition", "Elements")]
    [InlineData("DiscriminatedObjectTypeDefinition", "Cases")]
    [InlineData("VersionTransition<string>", "PreviousValue")]
    [InlineData("SourceLocation", "FilePath")]
    public void RequiredMembers_AreEnforcedByTheCompiler(string typeName, string propertyName)
    {
        var compilation = RoslynCompilationFactory.Create($$"""
            using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

            public static class Usage
            {
                public static object Create() => new {{typeName}}();
            }
            """);

        Assert.Contains(compilation.GetDiagnostics(), diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            diagnostic.Id == "CS9035" &&
            diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains($".{propertyName}", StringComparison.Ordinal));
    }

    [Fact]
    public void Sdk_DoesNotDependOnTheToolHostOrMsbuild()
    {
        Assert.DoesNotContain(typeof(TypeDefinition).Assembly.GetReferencedAssemblies(),
            reference => reference.Name?.StartsWith("Microsoft.Build", StringComparison.Ordinal) == true ||
                reference.Name?.StartsWith("Microsoft.CodeAnalysis.Workspaces", StringComparison.Ordinal) == true ||
                reference.Name == "Azure.Deployments.Extensibility.TypeGeneration.Tool");
    }

    private static void AssertSemanticFieldType(Type type)
    {
        if (type.IsGenericParameter || type == typeof(string) || type == typeof(bool) ||
            type == typeof(int) || type == typeof(long))
        {
            return;
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            AssertSemanticFieldType(underlying);
            return;
        }

        if (type.Namespace == typeof(TypeDefinition).Namespace)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                AssertSemanticFieldType(argument);
            }
            return;
        }

        Assert.True(type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(ImmutableArray<>) ||
                type.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>)),
            $"ETSM field contains an unexpected type: {type.FullName}");
        foreach (var argument in type.GetGenericArguments())
        {
            AssertSemanticFieldType(argument);
        }
    }

    private sealed record Member(string Name, Type Type, bool Required = false);
}
