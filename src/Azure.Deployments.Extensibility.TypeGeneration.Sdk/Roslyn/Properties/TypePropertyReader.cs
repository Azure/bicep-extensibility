// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Bicep.Types.Concrete;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Properties;

internal static class TypePropertyReader
{
    public static (string? Description, PropertyFlags Flags, bool IsSecure) Read(
        IPropertySymbol property,
        INamedTypeSymbol attributeType,
        SourceLocation? location,
        ImmutableArray<TypeGenDiagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        AttributeData? annotation = null;
        for (var current = property; current is not null; current = current.OverriddenProperty)
        {
            cancellationToken.ThrowIfCancellationRequested();
            annotation = current.GetAttributes().FirstOrDefault(attribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));
            if (annotation is not null ||
                (current.OverriddenProperty is { } overridden &&
                    !PropertySignatureComparer.HaveSameSignature(current, overridden)))
            {
                break;
            }
        }

        if (annotation is null)
        {
            return (null, PropertyFlags.None, false);
        }

        var description = annotation.ConstructorArguments[0].Value as string ?? string.Empty;
        var rawFlags = (ObjectTypePropertyFlags)(int)annotation.ConstructorArguments[1].Value!;
        var isSecure = (bool)annotation.ConstructorArguments[2].Value!;
        const ObjectTypePropertyFlags knownFlags = ObjectTypePropertyFlags.Required |
            ObjectTypePropertyFlags.ReadOnly | ObjectTypePropertyFlags.WriteOnly |
            ObjectTypePropertyFlags.DeployTimeConstant | ObjectTypePropertyFlags.Identifier;
        if ((rawFlags & ~knownFlags) != 0)
        {
            diagnostics.Add(new(TypeGenDiagnosticCodes.InvalidPropertyFlags, TypeGenDiagnosticSeverity.Error,
                $"Property '{property.Name}' uses unknown TypeProperty flag bits: {(int)(rawFlags & ~knownFlags)}.",
                annotation.ApplicationSyntaxReference is { } syntax
                    ? SourceLocationConverter.FromLocation(syntax.GetSyntax(cancellationToken).GetLocation())
                    : location));
        }

        var flags = PropertyFlags.None;
        if (rawFlags.HasFlag(ObjectTypePropertyFlags.Required))
        {
            flags |= PropertyFlags.Required;
        }
        if (rawFlags.HasFlag(ObjectTypePropertyFlags.ReadOnly))
        {
            flags |= PropertyFlags.ReadOnly;
        }
        if (rawFlags.HasFlag(ObjectTypePropertyFlags.WriteOnly))
        {
            flags |= PropertyFlags.WriteOnly;
        }
        if (rawFlags.HasFlag(ObjectTypePropertyFlags.DeployTimeConstant))
        {
            flags |= PropertyFlags.DeployTimeConstant;
        }
        if (rawFlags.HasFlag(ObjectTypePropertyFlags.Identifier))
        {
            flags |= PropertyFlags.Identifier;
        }

        return (description, flags, isSecure);
    }
}
