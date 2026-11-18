// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Validation;

internal static class PhaseOneModelValidator
{
    public static ImmutableArray<TypeGenDiagnostic> Validate(
        ExtensionTypeSemanticModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = ImmutableArray.CreateBuilder<TypeGenDiagnostic>();
        var visited = new HashSet<TypeDefinition>(ReferenceEqualityComparer.Instance);
        var pending = new Queue<(TypeDefinition? Type, SourceLocation? Location, bool AllowLiteral, bool AllowNull)>();

        Check(!string.IsNullOrWhiteSpace(model.Name) && !string.IsNullOrWhiteSpace(model.Version),
            "Extension name and version must be populated.", model.SourceLocation);
        Check(!model.IsPreview && !model.IsDeprecated && model.FallbackResourceType is null,
            "Phase 1 does not populate preview, deprecated, or fallback metadata.", model.SourceLocation);
        Check(!model.ResourceTypes.IsDefault, "ResourceTypes must be initialized.", model.SourceLocation);
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (!model.ResourceTypes.IsDefault)
        {
            foreach (var resource in model.ResourceTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (resource is null)
                {
                    Check(false, "ResourceTypes cannot contain null.", model.SourceLocation);
                    continue;
                }

                var location = resource.SourceLocation ?? model.SourceLocation;
                Check(!string.IsNullOrWhiteSpace(resource.Name) && names.Add(resource.Name),
                    "Resource names must be nonblank and unique.", location);
                Check(!resource.ApiVersions.IsDefault, "ApiVersions must be initialized.", location);
                var versions = new HashSet<string>(StringComparer.Ordinal);
                if (!resource.ApiVersions.IsDefault)
                {
                    foreach (var version in resource.ApiVersions)
                    {
                        Check(!string.IsNullOrWhiteSpace(version) && versions.Add(version),
                            "API-version labels must be nonblank and unique.", location);
                    }
                }

                Check(resource.ReadableScopes == ResourceScope.All && resource.WritableScopes == ResourceScope.All,
                    "Phase 1 resources must use All readable and writable scopes.", location);
                Check(!resource.ResourceFunctions.IsDefault && resource.ResourceFunctions.IsEmpty,
                    "Phase 1 resource functions must be initialized and empty.", location);
                AddRoot(resource.Body, location);
            }
        }

        if (model.Configuration is { } configuration)
        {
            AddRoot(configuration, model.SourceLocation);
        }

        while (pending.TryDequeue(out var item))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var location = item.Type?.SourceLocation ?? item.Location;
            if (item.Type is null)
            {
                Check(false, "A type edge cannot be null.", location);
                continue;
            }

            if (item.Type is NullTypeDefinition)
            {
                Check(item.AllowNull, "Null is only valid as a nullable-union element.", location);
                continue;
            }

            if (item.Type is StringLiteralTypeDefinition literal)
            {
                Check(item.AllowLiteral && !string.IsNullOrEmpty(literal.Value),
                    "String literals must be named enum-union elements.", location);
                continue;
            }

            if (!visited.Add(item.Type))
            {
                continue;
            }

            switch (item.Type)
            {
                case StringTypeDefinition text:
                    Check(text.MinLength is null && text.MaxLength is null && text.Pattern is null,
                        "Phase 1 strings cannot contain future constraints.", location);
                    break;
                case BooleanTypeDefinition:
                    break;
                case IntegerTypeDefinition integer:
                    Check(integer.MinValue is not null && integer.MaxValue is not null &&
                        integer.MinValue <= integer.MaxValue,
                        "Integer bounds must be initialized and ordered.", location);
                    break;
                case ArrayTypeDefinition array:
                    Check(array.MinLength is null && array.MaxLength is null,
                        "Phase 1 arrays cannot contain future constraints.", location);
                    Add(array.ItemType, location);
                    break;
                case ObjectTypeDefinition obj:
                    Check(!string.IsNullOrWhiteSpace(obj.Name), "Object names must be populated.", location);
                    Check(!obj.Sensitive, "Phase 1 does not mark entire objects sensitive.", location);
                    Check(!obj.Properties.IsDefault, "Object properties must be initialized.", location);
                    Check(obj.AdditionalProperties is null || obj.Properties.IsDefaultOrEmpty,
                        "Phase 1 maps cannot also contain fixed properties.", location);
                    var propertyNames = new HashSet<string>(StringComparer.Ordinal);
                    if (!obj.Properties.IsDefault)
                    {
                        foreach (var property in obj.Properties)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (property is null)
                            {
                                Check(false, "Properties cannot contain null.", location);
                                continue;
                            }

                            var propertyLocation = property.SourceLocation ?? location;
                            Check(!string.IsNullOrWhiteSpace(property.Name) && propertyNames.Add(property.Name),
                                "Effective property names must be nonblank and unique.", propertyLocation);
                            const PropertyFlags knownFlags = PropertyFlags.Required | PropertyFlags.ReadOnly |
                                PropertyFlags.WriteOnly | PropertyFlags.DeployTimeConstant | PropertyFlags.Identifier;
                            Check((property.Flags & ~knownFlags) == 0, "Property flags contain unknown bits.", propertyLocation);
                            Check(property.Versioning is { } versioning && IsEmpty(versioning.Availability) &&
                                versioning.NameChangedFrom is null && versioning.TypeChangedFrom is null &&
                                versioning.RequiredChangedFrom is null,
                                "Phase 1 property versioning must remain empty.", propertyLocation);
                            Add(property.Type, propertyLocation);
                        }
                    }

                    if (obj.AdditionalProperties is { } additional)
                    {
                        Add(additional, location);
                    }
                    break;
                case UnionTypeDefinition union:
                    if (union.Elements.IsDefaultOrEmpty)
                    {
                        Check(false, "Union elements must be initialized and nonempty.", location);
                        break;
                    }

                    var enumUnion = IsEnum(union);
                    var nullableUnion = union.Elements.Length == 2 &&
                        union.Elements.Count(element => element?.Type is NullTypeDefinition) == 1 &&
                        union.Elements.All(element => element?.Type is not UnionTypeDefinition inner || IsEnum(inner));
                    Check(enumUnion || nullableUnion, "Phase 1 unions must represent enums or nullable occurrences.", location);
                    var literals = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var element in union.Elements)
                    {
                        if (element is null)
                        {
                            Check(false, "Union elements cannot contain null.", location);
                            continue;
                        }

                        var elementLocation = element.SourceLocation ?? location;
                        Check(IsEmpty(element.Availability), "Phase 1 union availability must remain empty.", elementLocation);
                        if (element.Type is StringLiteralTypeDefinition value)
                        {
                            Check(literals.Add(value.Value), "Enum literal values must be unique.", elementLocation);
                        }
                        pending.Enqueue((element.Type, elementLocation, enumUnion, nullableUnion));
                    }
                    break;
                default:
                    Check(false, $"Type '{item.Type.GetType().Name}' is outside the Phase 1 normalized surface.", location);
                    break;
            }
        }

        return diagnostics.ToImmutable();

        void Check(bool condition, string message, SourceLocation? location)
        {
            if (!condition)
            {
                diagnostics.Add(new(TypeGenDiagnosticCodes.InvalidSemanticModel, TypeGenDiagnosticSeverity.Error, message, location));
            }
        }

        void Add(TypeDefinition? type, SourceLocation? location) => pending.Enqueue((type, location, false, false));

        void AddRoot(TypeDefinition? type, SourceLocation? location)
        {
            Check(type is ObjectTypeDefinition { AdditionalProperties: null },
                "Resource and configuration roots must be object schemas, not maps or scalar types.", type?.SourceLocation ?? location);
            Add(type, location);
        }
    }

    private static bool IsEmpty(VersionAvailability? availability)
        => availability is { AddedAt: null, RemovedAt: null };

    private static bool IsEnum(UnionTypeDefinition union)
        => !union.Elements.IsDefaultOrEmpty && union.Elements.All(element => element?.Type is StringLiteralTypeDefinition);
}
