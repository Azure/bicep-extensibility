// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Lowering;

internal static class TypeGenerationLowerer
{
    public static TypeGenerationLoweringResult Lower(
        TypeGenerationProjectedExtension model,
        CancellationToken cancellationToken = default)
    {
        var diagnostics = TypeGenerationModelValidator.ValidateProjected(model, cancellationToken).ToBuilder();
        cancellationToken.ThrowIfCancellationRequested();
        if (diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            return new(null, diagnostics.ToImmutable());
        }

        var resources = model.ResourceTypes.Select(resource =>
            (Key: resource.ApiVersion is null ? resource.Name : $"{resource.Name}@{resource.ApiVersion}", Resource: resource))
            .OrderBy(item => item.Key, StringComparer.Ordinal).ToArray();
        foreach (var group in resources.GroupBy(item => item.Key, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            foreach (var item in group)
            {
                cancellationToken.ThrowIfCancellationRequested();
                diagnostics.Add(new(TypeGenDiagnosticCodes.BackendResourceIdentityCollision, TypeGenDiagnosticSeverity.Error,
                    $"More than one projected resource encodes to public resource key '{group.Key}'.",
                    item.Resource.SourceLocation ?? model.SourceLocation));
            }
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            return new(null, TypeGenerationModelValidator.OrderDiagnostics(diagnostics));
        }

        var context = new LoweringContext(cancellationToken);
        if (model.Configuration is { } configuration)
        {
            context.RegisterGraph(configuration);
        }
        foreach (var item in resources)
        {
            context.RegisterGraph(item.Resource.Body);
        }

        var types = context.LowerTypes();
        var entries = ImmutableSortedDictionary.CreateBuilder<string, CrossFileTypeReference>(StringComparer.Ordinal);
        foreach (var item in resources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add(item.Key, new(TypeGenerationLoweredPackage.TypesFileName, types.Count));
            types.Add(new ResourceType(item.Key, context.Reference(item.Resource.Body), functions: null,
                writableScopes_in: MapScope(item.Resource.WritableScopes),
                readableScopes_in: MapScope(item.Resource.ReadableScopes)));
        }

        var settings = new TypeSettings(model.Name, model.Version, model.IsSingleton,
            isPreview: null, isDeprecated: null,
            configurationType: model.Configuration is { } config
                ? new(TypeGenerationLoweredPackage.TypesFileName, context.Index(config))
                : null);
        var index = new TypeIndex(entries.ToImmutable(),
            ImmutableDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>>.Empty,
            ImmutableArray<CrossFileTypeReference>.Empty, settings, fallbackResourceType: null);
        var lowered = types.ToImmutable();
        context.Hydrate(lowered);
        cancellationToken.ThrowIfCancellationRequested();
        return new(new TypeGenerationLoweredPackage(index, lowered), diagnostics.ToImmutable());
    }

    private static ScopeType MapScope(ResourceScope scope)
    {
        var result = ScopeType.None;
        if ((scope & ResourceScope.Tenant) != 0) { result |= ScopeType.Tenant; }
        if ((scope & ResourceScope.ManagementGroup) != 0) { result |= ScopeType.ManagementGroup; }
        if ((scope & ResourceScope.Subscription) != 0) { result |= ScopeType.Subscription; }
        if ((scope & ResourceScope.ResourceGroup) != 0) { result |= ScopeType.ResourceGroup; }
        if ((scope & ResourceScope.Extension) != 0) { result |= ScopeType.Extension; }
        return result;
    }

    private static ObjectTypePropertyFlags MapFlags(PropertyFlags flags)
    {
        var result = ObjectTypePropertyFlags.None;
        if ((flags & PropertyFlags.Required) != 0) { result |= ObjectTypePropertyFlags.Required; }
        if ((flags & PropertyFlags.ReadOnly) != 0) { result |= ObjectTypePropertyFlags.ReadOnly; }
        if ((flags & PropertyFlags.WriteOnly) != 0) { result |= ObjectTypePropertyFlags.WriteOnly; }
        if ((flags & PropertyFlags.DeployTimeConstant) != 0) { result |= ObjectTypePropertyFlags.DeployTimeConstant; }
        if ((flags & PropertyFlags.Identifier) != 0) { result |= ObjectTypePropertyFlags.Identifier; }
        return result;
    }

    private sealed class LoweringContext(CancellationToken cancellationToken)
    {
        private readonly Dictionary<TypeDefinition, int> indices = new(ReferenceEqualityComparer.Instance);
        private readonly List<TypeDefinition> nodes = [];
        private readonly TypeFactory factory = new([]);

        public void RegisterGraph(TypeDefinition root)
        {
            var pending = new Stack<TypeDefinition>();
            pending.Push(root);
            while (pending.TryPop(out var type))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!indices.TryAdd(type, nodes.Count))
                {
                    continue;
                }

                nodes.Add(type);
                foreach (var child in Children(type).Reverse())
                {
                    pending.Push(child);
                }
            }
        }

        public int Index(TypeDefinition type) => indices[type];

        public ITypeReference Reference(TypeDefinition type) => factory.GetReference(Index(type));

        public void Hydrate(ImmutableArray<TypeBase> types) => factory.Hydrate(types);

        public ImmutableArray<TypeBase>.Builder LowerTypes()
        {
            var types = ImmutableArray.CreateBuilder<TypeBase>();
            foreach (var node in nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                types.Add(node switch
                {
                    AnyTypeDefinition => new AnyType(),
                    NullTypeDefinition => new NullType(),
                    BooleanTypeDefinition => new BooleanType(),
                    IntegerTypeDefinition value => new IntegerType(value.MinValue, value.MaxValue),
                    StringTypeDefinition value => new StringType(value.Sensitive ? true : null, value.MinLength, value.MaxLength, value.Pattern),
                    StringLiteralTypeDefinition value => new StringLiteralType(value.Value),
                    ArrayTypeDefinition value => new ArrayType(Reference(value.ItemType), value.MinLength, value.MaxLength),
                    ObjectTypeDefinition value => LowerObject(value),
                    UnionTypeDefinition value => new UnionType(value.Elements.Select(element => Reference(element.Type)).ToImmutableArray()),
                    _ => throw new InvalidOperationException($"Validated type '{node.GetType().Name}' has no lowering mapping."),
                });
            }

            return types;
        }

        private ObjectType LowerObject(ObjectTypeDefinition value)
        {
            var properties = ImmutableSortedDictionary.CreateBuilder<string, ObjectTypeProperty>(StringComparer.Ordinal);
            foreach (var property in value.Properties)
            {
                cancellationToken.ThrowIfCancellationRequested();
                properties.Add(property.Name, new(Reference(property.Type), MapFlags(property.Flags), property.Description));
            }

            return new(value.Name, properties.ToImmutable(),
                value.AdditionalProperties is { } additional ? Reference(additional) : null,
                value.Sensitive ? true : null);
        }

        private static IEnumerable<TypeDefinition> Children(TypeDefinition type)
        {
            switch (type)
            {
                case ArrayTypeDefinition array:
                    yield return array.ItemType;
                    break;
                case ObjectTypeDefinition obj:
                    foreach (var property in obj.Properties.OrderBy(property => property.Name, StringComparer.Ordinal))
                    {
                        yield return property.Type;
                    }
                    if (obj.AdditionalProperties is { } additional)
                    {
                        yield return additional;
                    }
                    break;
                case UnionTypeDefinition union:
                    foreach (var element in union.Elements)
                    {
                        yield return element.Type;
                    }
                    break;
            }
        }
    }
}
