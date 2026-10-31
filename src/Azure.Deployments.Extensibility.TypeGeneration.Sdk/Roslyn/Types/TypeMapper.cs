// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Properties;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Types;

internal sealed class TypeMapper(
    Compilation compilation,
    AuthoringSymbols symbols,
    ImmutableArray<TypeGenDiagnostic>.Builder diagnostics,
    CancellationToken cancellationToken)
{
    private const int MaximumDepth = 128;
    private readonly Dictionary<ITypeSymbol, TypePlan> plans = new(SymbolEqualityComparer.IncludeNullability);
    private readonly Dictionary<(TypePlan Plan, bool Secure), TypeDefinition> nodes = [];
    private readonly HashSet<TypeGenDiagnostic> reportedErrors = [];
    private readonly INamedTypeSymbol enumerable = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
    private readonly INamedTypeSymbol? dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2");
    private readonly INamedTypeSymbol? readOnlyDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2");
    private readonly INamedTypeSymbol? nonGenericEnumerable = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");

    public TypePlan ValidateRoot(INamedTypeSymbol schema, SourceLocation? location)
    {
        if (schema.TypeKind != TypeKind.Class || schema.IsStatic || ContainsTypeParameter(schema))
        {
            Error(TypeGenDiagnosticCodes.UnsupportedSchema,
                $"Root schema '{schema.ToDisplayString()}' must be a non-static class without open type parameters.", location);
            return new(schema, location, schema.Name);
        }

        var plan = GetPlan(schema.WithNullableAnnotation(NullableAnnotation.NotAnnotated),
            location, schema.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), 0);
        if (plan.Kind != Shape.Object)
        {
            Error(TypeGenDiagnosticCodes.UnsupportedSchema,
                $"Root schema '{schema.ToDisplayString()}' must be an object class or record, not a collection or scalar.", location);
        }

        return plan;
    }

    public TypeDefinition Build(TypePlan plan) => Build(plan, false);

    private TypePlan GetPlan(ITypeSymbol type, SourceLocation? location, string path, int depth)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.None)
        {
            diagnostics.Add(new(TypeGenDiagnosticCodes.ObliviousNullability, TypeGenDiagnosticSeverity.Warning,
                $"Type '{type.ToDisplayString()}' has no nullable annotation. Its legacy non-null shape is preserved; enable nullable annotations to specify nullability.",
                location));
            type = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        if (plans.TryGetValue(type, out var existing))
        {
            foreach (var error in existing.OccurrenceErrors)
            {
                Error(error.Code, error.Message, location);
            }
            return existing;
        }

        var plan = new TypePlan(type, location, path);
        if (depth >= MaximumDepth)
        {
            Fail(plan, TypeGenDiagnosticCodes.TypeGraphTooDeep,
                $"The type graph exceeds {MaximumDepth} nested shapes, possibly due to an expanding generic recursion.", location);
            return plan;
        }

        // Register before following edges: a repeated active symbol is a graph edge, not another traversal.
        plans.Add(type, plan);
        if (type is INamedTypeSymbol nullable &&
            nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            plan.Kind = Shape.Nullable;
            plan.Item = GetPlan(nullable.TypeArguments[0], location, path + ".value", depth + 1);
            plan.OccurrenceErrors = plan.Item.OccurrenceErrors;
            return plan;
        }

        if (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            plan.Kind = Shape.Nullable;
            plan.Item = GetPlan(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated), location, path, depth + 1);
            plan.OccurrenceErrors = plan.Item.OccurrenceErrors;
            return plan;
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_String:
                plan.Kind = Shape.String;
                return plan;
            case SpecialType.System_Boolean:
                plan.Kind = Shape.Boolean;
                return plan;
            case SpecialType.System_SByte:
                return Integer(plan, sbyte.MinValue, sbyte.MaxValue);
            case SpecialType.System_Byte:
                return Integer(plan, byte.MinValue, byte.MaxValue);
            case SpecialType.System_Int16:
                return Integer(plan, short.MinValue, short.MaxValue);
            case SpecialType.System_UInt16:
                return Integer(plan, ushort.MinValue, ushort.MaxValue);
            case SpecialType.System_Int32:
                return Integer(plan, int.MinValue, int.MaxValue);
            case SpecialType.System_UInt32:
                return Integer(plan, uint.MinValue, uint.MaxValue);
            case SpecialType.System_Int64:
                return Integer(plan, long.MinValue, long.MaxValue);
            case SpecialType.System_UInt64:
                Fail(plan, TypeGenDiagnosticCodes.UnsupportedIntegerRange,
                    "The ulong range cannot be represented by the ETSM signed long bounds.", location);
                return plan;
        }

        if (type is IArrayTypeSymbol array)
        {
            if (array.Rank != 1 || !array.IsSZArray)
            {
                Fail(plan, TypeGenDiagnosticCodes.UnsupportedType, "Only single-dimensional vector arrays are supported.", location);
                return plan;
            }

            plan.Kind = Shape.Array;
            plan.Item = GetPlan(array.ElementType, location, path + "[]", depth + 1);
            plan.OccurrenceErrors = plan.Item.OccurrenceErrors;
            return plan;
        }

        if (type is not INamedTypeSymbol named || named.IsUnboundGenericType || ContainsTypeParameter(named))
        {
            Unsupported(plan, location);
            return plan;
        }

        if (named.TypeKind == TypeKind.Enum)
        {
            plan.Kind = Shape.Enum;
            plan.EnumMembers = named.GetMembers().OfType<IFieldSymbol>()
                .Where(field => field.HasConstantValue)
                .OrderBy(field => EnumOrder(field.ConstantValue!))
                .ToImmutableArray();
            if (plan.EnumMembers.IsEmpty)
            {
                Fail(plan, TypeGenDiagnosticCodes.UnsupportedType,
                    $"Enum '{named.ToDisplayString()}' has no named values to export.", location);
            }

            return plan;
        }

        var candidates = named.AllInterfaces.Prepend(named).ToArray();
        var maps = candidates.Where(candidate =>
            SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, dictionary) ||
            SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, readOnlyDictionary)).ToArray();
        if (maps.Length > 0)
        {
            if (maps.Any(map => map.TypeArguments[0].SpecialType != SpecialType.System_String))
            {
                Fail(plan, TypeGenDiagnosticCodes.InvalidDictionaryKey, "Dictionary keys must be strings.", location);
                return plan;
            }

            var values = maps.Select(map => map.TypeArguments[1])
                .Distinct<ITypeSymbol>(SymbolEqualityComparer.IncludeNullability).ToArray();
            if (values.Length != 1)
            {
                Fail(plan, TypeGenDiagnosticCodes.AmbiguousCollection, "Dictionary interfaces expose different value types.", location);
                return plan;
            }

            plan.Kind = Shape.Map;
            plan.Item = GetPlan(values[0], location, path + ".value", depth + 1);
            plan.OccurrenceErrors = plan.Item.OccurrenceErrors;
            return plan;
        }

        var elements = candidates.Where(candidate =>
                SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, enumerable))
            .Select(candidate => candidate.TypeArguments[0])
            .Distinct<ITypeSymbol>(SymbolEqualityComparer.IncludeNullability).ToArray();
        if (elements.Length > 0)
        {
            if (elements.Length != 1)
            {
                Fail(plan, TypeGenDiagnosticCodes.AmbiguousCollection, "Collection interfaces expose different element types.", location);
                return plan;
            }

            plan.Kind = Shape.Array;
            plan.Item = GetPlan(elements[0], location, path + "[]", depth + 1);
            plan.OccurrenceErrors = plan.Item.OccurrenceErrors;
            return plan;
        }

        if (candidates.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, nonGenericEnumerable)) ||
            named.TypeKind != TypeKind.Class || named.IsStatic || named.SpecialType != SpecialType.None)
        {
            Unsupported(plan, location);
            return plan;
        }

        plan.Kind = Shape.Object;
        var properties = EffectivePropertyCollector.Collect(named, symbols, diagnostics, cancellationToken);
        plan.Properties = properties.Select(property => (property, GetPlan(
            property.Symbol.Type, property.SourceLocation ?? location, path + "." + property.Name, depth + 1))).ToImmutableArray();
        return plan;
    }

    private TypeDefinition Build(TypePlan plan, bool secure)
    {
        cancellationToken.ThrowIfCancellationRequested();
        secure &= plan.Kind is Shape.String or Shape.Array or Shape.Nullable;
        var key = (plan, secure);
        if (nodes.TryGetValue(key, out var existing))
        {
            return existing;
        }

        TypeDefinition node = plan.Kind switch
        {
            Shape.String => new StringTypeDefinition { Sensitive = secure, SourceLocation = plan.Location },
            Shape.Boolean => new BooleanTypeDefinition { SourceLocation = plan.Location },
            Shape.Integer => new IntegerTypeDefinition
            {
                MinValue = plan.MinValue,
                MaxValue = plan.MaxValue,
                SourceLocation = plan.Location,
            },
            Shape.Nullable => new UnionTypeDefinition(current =>
            {
                nodes.Add(key, current);
                return
                [
                    new() { Type = Build(plan.Item!, secure), SourceLocation = plan.Location },
                    new() { Type = new NullTypeDefinition { SourceLocation = plan.Location }, SourceLocation = plan.Location },
                ];
            }) { SourceLocation = plan.Location },
            Shape.Array => new ArrayTypeDefinition(current =>
            {
                nodes.Add(key, current);
                return Build(plan.Item!, secure);
            }) { SourceLocation = plan.Location },
            Shape.Map => new ObjectTypeDefinition($"Map:{plan.Path}", current =>
            {
                nodes.Add(key, current);
                return ([], Build(plan.Item!, false));
            }) { SourceLocation = plan.Location },
            Shape.Object => new ObjectTypeDefinition(plan.Symbol.Name, current =>
            {
                nodes.Add(key, current);
                return (plan.Properties.Select(property => new PropertyDefinition
                {
                    Name = property.Property.Name,
                    Type = Build(property.Plan, property.Property.IsSecure),
                    Description = property.Property.Description,
                    Flags = property.Property.Flags,
                    SourceLocation = property.Property.SourceLocation ?? plan.Location,
                }).ToImmutableArray(), null);
            }) { SourceLocation = SourceLocationConverter.FromSymbol(plan.Symbol) ?? plan.Location },
            Shape.Enum => new UnionTypeDefinition
            {
                Elements = plan.EnumMembers.Select(member => new UnionElementDefinition
                {
                    Type = new StringLiteralTypeDefinition { Value = member.Name, SourceLocation = SourceLocationConverter.FromSymbol(member) },
                    SourceLocation = SourceLocationConverter.FromSymbol(member),
                }).ToImmutableArray(),
                SourceLocation = SourceLocationConverter.FromSymbol(plan.Symbol) ?? plan.Location,
            },
            _ => throw new InvalidOperationException("An invalid type plan cannot be materialized. Validate all roots before building."),
        };
        nodes[key] = node;
        return node;
    }

    private static TypePlan Integer(TypePlan plan, long min, long max)
    {
        plan.Kind = Shape.Integer;
        plan.MinValue = min;
        plan.MaxValue = max;
        return plan;
    }

    private static bool ContainsTypeParameter(ITypeSymbol type)
        => type is ITypeParameterSymbol ||
            type is IArrayTypeSymbol array && ContainsTypeParameter(array.ElementType) ||
            type is INamedTypeSymbol named &&
            (named.TypeArguments.Any(ContainsTypeParameter) ||
                named.ContainingType is { } containing && ContainsTypeParameter(containing));

    private static ulong EnumOrder(object value) => value switch
    {
        sbyte number => unchecked((ulong)number),
        short number => unchecked((ulong)number),
        int number => unchecked((ulong)number),
        long number => unchecked((ulong)number),
        _ => Convert.ToUInt64(value, System.Globalization.CultureInfo.InvariantCulture),
    };

    private void Unsupported(TypePlan plan, SourceLocation? location)
        => Fail(plan, TypeGenDiagnosticCodes.UnsupportedType, $"Unsupported schema type '{plan.Symbol.ToDisplayString()}'.", location);

    private void Fail(TypePlan plan, string code, string message, SourceLocation? location)
    {
        plan.OccurrenceErrors = [(code, message)];
        Error(code, message, location);
    }

    private void Error(string code, string message, SourceLocation? location)
    {
        var diagnostic = new TypeGenDiagnostic(code, TypeGenDiagnosticSeverity.Error, message, location);
        if (reportedErrors.Add(diagnostic))
        {
            diagnostics.Add(diagnostic);
        }
    }

    internal enum Shape { Unsupported, String, Boolean, Integer, Nullable, Array, Map, Enum, Object }

    internal sealed class TypePlan(ITypeSymbol symbol, SourceLocation? location, string path)
    {
        public ITypeSymbol Symbol { get; } = symbol;
        public SourceLocation? Location { get; } = location;
        public string Path { get; } = path;
        public Shape Kind { get; set; }
        public long? MinValue { get; set; }
        public long? MaxValue { get; set; }
        public TypePlan? Item { get; set; }
        public ImmutableArray<(EffectiveProperty Property, TypePlan Plan)> Properties { get; set; } = [];
        public ImmutableArray<IFieldSymbol> EnumMembers { get; set; } = [];
        public ImmutableArray<(string Code, string Message)> OccurrenceErrors { get; set; } = [];
    }
}
