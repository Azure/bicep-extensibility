// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Properties;

internal static class PropertySignatureComparer
{
    public static bool HaveSameSignature(IPropertySymbol first, IPropertySymbol second)
        => first.MetadataName == second.MetadataName &&
            HaveSameRuntimeType(first.Type, second.Type) &&
            (first.RefKind == RefKind.None) == (second.RefKind == RefKind.None) &&
            first.Parameters.Length == second.Parameters.Length &&
            first.Parameters.Zip(second.Parameters).All(pair =>
                HaveSameRuntimeType(pair.First.Type, pair.Second.Type));

    // Reflection erases dynamic to object, including inside arrays and generic arguments.
    private static bool HaveSameRuntimeType(ITypeSymbol first, ITypeSymbol second)
    {
        if (SymbolEqualityComparer.Default.Equals(first, second))
        {
            return true;
        }

        if ((first.TypeKind == TypeKind.Dynamic && second.SpecialType == SpecialType.System_Object) ||
            (second.TypeKind == TypeKind.Dynamic && first.SpecialType == SpecialType.System_Object))
        {
            return true;
        }

        if (first is IArrayTypeSymbol firstArray && second is IArrayTypeSymbol secondArray)
        {
            return firstArray.Rank == secondArray.Rank && firstArray.IsSZArray == secondArray.IsSZArray &&
                HaveSameRuntimeType(firstArray.ElementType, secondArray.ElementType);
        }

        if (first is INamedTypeSymbol firstNamed && second is INamedTypeSymbol secondNamed &&
            SymbolEqualityComparer.Default.Equals(firstNamed.OriginalDefinition, secondNamed.OriginalDefinition))
        {
            return firstNamed.TypeArguments.Zip(secondNamed.TypeArguments)
                .All(pair => HaveSameRuntimeType(pair.First, pair.Second)) &&
                (firstNamed.ContainingType is null || (secondNamed.ContainingType is { } secondContaining &&
                    HaveSameRuntimeType(firstNamed.ContainingType, secondContaining)));
        }

        return false;
    }
}
