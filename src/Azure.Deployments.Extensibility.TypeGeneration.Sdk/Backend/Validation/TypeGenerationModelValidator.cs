// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;

public static class TypeGenerationModelValidator
{
    public static ImmutableArray<TypeGenDiagnostic> Validate(
        ExtensionTypeSemanticModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();
        return new ValidationContext(cancellationToken).Validate(model);
    }

    internal static ImmutableArray<TypeGenDiagnostic> ValidateProjected(
        TypeGenerationProjectedExtension model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();
        return new ValidationContext(cancellationToken).ValidateProjected(model);
    }

    internal static ImmutableArray<TypeGenDiagnostic> OrderDiagnostics(IEnumerable<TypeGenDiagnostic> diagnostics) =>
        diagnostics.OrderBy(diagnostic => diagnostic.SourceLocation?.FilePath ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.SourceLocation?.Start ?? -1)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.SourceLocation?.Length ?? -1)
            .ToImmutableArray();

    private sealed class ValidationContext(CancellationToken cancellationToken)
    {
        private readonly ImmutableArray<TypeGenDiagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<TypeGenDiagnostic>();
        private readonly HashSet<TypeDefinition> visited = new(ReferenceEqualityComparer.Instance);
        private readonly Queue<(TypeDefinition? Type, SourceLocation? Location, string Context)> pending = new();

        public ImmutableArray<TypeGenDiagnostic> Validate(ExtensionTypeSemanticModel model)
        {
            CheckName(model.Name, TypeGenDiagnosticCodes.BackendInvalidMetadata, "Extension name", model.SourceLocation);
            CheckName(model.Version, TypeGenDiagnosticCodes.BackendInvalidMetadata, "Extension version", model.SourceLocation);
            if (model.IsPreview || model.IsDeprecated)
            {
                Error(TypeGenDiagnosticCodes.BackendUnsupportedSemantic,
                    "Non-default IsPreview or IsDeprecated cannot be represented in canonical extension settings.", model.SourceLocation);
            }

            if (model.FallbackResourceType is { } fallback)
            {
                Error(TypeGenDiagnosticCodes.BackendUnsupportedSemantic,
                    "Fallback resources are not supported by this backend.", fallback.SourceLocation ?? model.SourceLocation);
            }

            if (model.Configuration is { } configuration)
            {
                AddRoot(configuration, model.SourceLocation, "Extension configuration");
            }

            if (model.ResourceTypes.IsDefault)
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidGraph, "ResourceTypes must be initialized.", model.SourceLocation);
            }
            else
            {
                var registrations = new List<ResourceTypeDefinition>();
                foreach (var resource in model.ResourceTypes.OrderBy(resource => resource?.Name, StringComparer.Ordinal)
                    .ThenBy(resource => resource is null || resource.ApiVersions.IsDefaultOrEmpty ? null : resource.ApiVersions[0], StringComparer.Ordinal)
                    .ThenBy(resource => resource?.SourceLocation?.FilePath, StringComparer.Ordinal)
                    .ThenBy(resource => resource?.SourceLocation?.Start ?? -1))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (resource is null)
                    {
                        Error(TypeGenDiagnosticCodes.BackendInvalidGraph, "ResourceTypes cannot contain null.", model.SourceLocation);
                        continue;
                    }

                    var location = resource.SourceLocation ?? model.SourceLocation;
                    var validName = CheckName(resource.Name, TypeGenDiagnosticCodes.BackendInvalidResource, "Resource name", location);
                    var validVersions = ValidateVersions(resource, location);
                    if (validName && validVersions)
                    {
                        registrations.Add(resource);
                    }

                    ValidateScopes(resource.Name, resource.ReadableScopes, resource.WritableScopes, location);

                    if (resource.ResourceFunctions.IsDefault)
                    {
                        Error(TypeGenDiagnosticCodes.BackendInvalidGraph,
                            $"Resource '{resource.Name}' functions must be initialized.", location);
                    }
                    else
                    {
                        foreach (var function in resource.ResourceFunctions)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (function is null)
                            {
                                Error(TypeGenDiagnosticCodes.BackendInvalidGraph,
                                    $"Resource '{resource.Name}' functions cannot contain null.", location);
                                continue;
                            }

                            Error(TypeGenDiagnosticCodes.BackendUnsupportedSemantic,
                                $"Resource '{resource.Name}' function '{function.Name}' is not supported in Extension Resource type packages.",
                                function.SourceLocation ?? location);
                        }
                    }

                    AddRoot(resource.Body, location, $"Resource '{resource.Name}' body");
                }

                ValidateRegistrations(registrations, model.SourceLocation);
            }

            return CompleteValidation();
        }

        public ImmutableArray<TypeGenDiagnostic> ValidateProjected(TypeGenerationProjectedExtension model)
        {
            CheckName(model.Name, TypeGenDiagnosticCodes.BackendInvalidMetadata, "Extension name", model.SourceLocation);
            CheckName(model.Version, TypeGenDiagnosticCodes.BackendInvalidMetadata, "Extension version", model.SourceLocation);
            if (model.Configuration is { } configuration)
            {
                AddRoot(configuration, model.SourceLocation, "Extension configuration");
            }

            foreach (var resource in model.ResourceTypes.OrderBy(resource => resource.Name, StringComparer.Ordinal)
                .ThenBy(resource => resource.ApiVersion, StringComparer.Ordinal)
                .ThenBy(resource => resource.SourceLocation?.FilePath, StringComparer.Ordinal)
                .ThenBy(resource => resource.SourceLocation?.Start ?? -1))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var location = resource.SourceLocation ?? model.SourceLocation;
                CheckName(resource.Name, TypeGenDiagnosticCodes.BackendInvalidResource, "Resource name", location);
                if (resource.ApiVersion is not null)
                {
                    CheckName(resource.ApiVersion, TypeGenDiagnosticCodes.BackendInvalidApiVersions,
                        $"Resource '{resource.Name}' API version", location);
                }

                ValidateScopes(resource.Name, resource.ReadableScopes, resource.WritableScopes, location);
                if (resource.ResourceFunctions.IsDefault)
                {
                    Error(TypeGenDiagnosticCodes.BackendInvalidGraph,
                        $"Resource '{resource.Name}' functions must be initialized.", location);
                }
                else
                {
                    foreach (var function in resource.ResourceFunctions)
                    {
                        if (function is null)
                        {
                            Error(TypeGenDiagnosticCodes.BackendInvalidGraph,
                                $"Resource '{resource.Name}' functions cannot contain null.", location);
                        }
                        else
                        {
                            Error(TypeGenDiagnosticCodes.BackendUnsupportedSemantic,
                                $"Resource '{resource.Name}' function '{function.Name}' is not supported in Extension Resource type packages.",
                                function.SourceLocation ?? location);
                        }
                    }
                }

                AddRoot(resource.Body, location, $"Resource '{resource.Name}' body");
            }

            foreach (var group in model.ResourceTypes.GroupBy(resource => resource.Name, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (group.Any(resource => resource.ApiVersion is null) && group.Any(resource => resource.ApiVersion is not null))
                {
                    foreach (var resource in group)
                    {
                        Error(TypeGenDiagnosticCodes.BackendConflictingResources,
                            $"Resource '{group.Key}' mixes versioned and unversioned definitions.", resource.SourceLocation ?? model.SourceLocation);
                    }
                }
            }

            return CompleteValidation();
        }

        private ImmutableArray<TypeGenDiagnostic> CompleteValidation()
        {
            while (pending.TryDequeue(out var item))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item.Type is null)
                {
                    Error(TypeGenDiagnosticCodes.BackendInvalidGraph,
                        $"{item.Context} contains a null type reference.", item.Location);
                    continue;
                }

                if (!visited.Add(item.Type))
                {
                    continue;
                }

                ValidateType(item.Type, item.Type.SourceLocation ?? item.Location, item.Context);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return OrderDiagnostics(diagnostics);
        }

        private void ValidateScopes(string name, ResourceScope readable, ResourceScope writable, SourceLocation? location)
        {
            if ((readable & ~ResourceScope.All) != 0 || (writable & ~ResourceScope.All) != 0)
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidScopes,
                    $"Resource '{name}' has unknown readable or writable scope bits.", location);
            }
        }

        private bool ValidateVersions(ResourceTypeDefinition resource, SourceLocation? location)
        {
            if (resource.ApiVersions.IsDefault)
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidApiVersions,
                    $"Resource '{resource.Name}' ApiVersions must be initialized; use an empty array for an unversioned resource.", location);
                return false;
            }

            var valid = true;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var version in resource.ApiVersions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(version) || !seen.Add(version))
                {
                    valid = false;
                    Error(TypeGenDiagnosticCodes.BackendInvalidApiVersions,
                        $"Resource '{resource.Name}' has a null, blank, or repeated API-version label '{version}'.", location);
                }
            }

            return valid;
        }

        private void ValidateRegistrations(List<ResourceTypeDefinition> registrations, SourceLocation? modelLocation)
        {
            foreach (var group in registrations.GroupBy(resource => resource.Name, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var unversioned = group.Where(resource => resource.ApiVersions.IsEmpty).ToArray();
                if (unversioned.Length > 0 && unversioned.Length != group.Count())
                {
                    foreach (var resource in group)
                    {
                        Error(TypeGenDiagnosticCodes.BackendConflictingResources,
                            $"Resource '{group.Key}' mixes versioned and unversioned definitions.", resource.SourceLocation ?? modelLocation);
                    }
                }

                if (unversioned.Length > 1)
                {
                    foreach (var resource in unversioned)
                    {
                        Error(TypeGenDiagnosticCodes.BackendConflictingResources,
                            $"Unversioned resource '{group.Key}' is registered more than once.", resource.SourceLocation ?? modelLocation);
                    }
                }

                foreach (var versions in group.SelectMany(resource => resource.ApiVersions.Select(version => (Resource: resource, Version: version)))
                    .GroupBy(item => item.Version, StringComparer.Ordinal).Where(versions => versions.Count() > 1))
                {
                    foreach (var item in versions)
                    {
                        Error(TypeGenDiagnosticCodes.BackendConflictingResources,
                            $"Resource '{group.Key}' API version '{versions.Key}' is registered more than once.",
                            item.Resource.SourceLocation ?? modelLocation);
                    }
                }
            }
        }

        private void AddRoot(TypeDefinition? type, SourceLocation? location, string context)
        {
            if (type is not (ObjectTypeDefinition or DiscriminatedObjectTypeDefinition))
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidRoot, $"{context} must be an object-like type.", type?.SourceLocation ?? location);
            }

            if (type is not null)
            {
                pending.Enqueue((type, location, context));
            }
        }

        private void ValidateType(TypeDefinition type, SourceLocation? location, string context)
        {
            switch (type)
            {
                case AnyTypeDefinition or NullTypeDefinition or BooleanTypeDefinition:
                    break;
                case IntegerTypeDefinition integer:
                    if (integer.MinValue is { } min && integer.MaxValue is { } max && min > max)
                    {
                        Error(TypeGenDiagnosticCodes.BackendInvalidConstraint, $"{context} has reversed integer bounds.", location);
                    }
                    break;
                case StringTypeDefinition text:
                    ValidateLengths(text.MinLength, text.MaxLength, location, context);
                    break;
                case StringLiteralTypeDefinition literal:
                    if (literal.Value is null)
                    {
                        Error(TypeGenDiagnosticCodes.BackendInvalidGraph, $"{context} has a null string literal value.", location);
                    }
                    break;
                case ArrayTypeDefinition array:
                    ValidateLengths(array.MinLength, array.MaxLength, location, context);
                    pending.Enqueue((array.ItemType, location, "Array item"));
                    break;
                case ObjectTypeDefinition obj:
                    ValidateObject(obj, location);
                    break;
                case UnionTypeDefinition union:
                    ValidateUnion(union, location);
                    break;
                default:
                    Error(TypeGenDiagnosticCodes.BackendUnsupportedType,
                        $"{context} uses unsupported semantic type '{type.GetType().Name}'.", location);
                    break;
            }
        }

        private void ValidateObject(ObjectTypeDefinition obj, SourceLocation? location)
        {
            CheckName(obj.Name, TypeGenDiagnosticCodes.BackendInvalidGraph, "Object type name", location);
            if (obj.Properties.IsDefault)
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidGraph, $"Object '{obj.Name}' properties must be initialized.", location);
            }
            else
            {
                var duplicateNames = obj.Properties.Where(property => property is not null && !string.IsNullOrWhiteSpace(property.Name))
                    .GroupBy(property => property.Name, StringComparer.Ordinal).Where(group => group.Count() > 1)
                    .Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
                foreach (var property in obj.Properties.OrderBy(property => property?.Name, StringComparer.Ordinal))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (property is null)
                    {
                        Error(TypeGenDiagnosticCodes.BackendInvalidGraph, $"Object '{obj.Name}' properties cannot contain null.", location);
                        continue;
                    }

                    var propertyLocation = property.SourceLocation ?? location;
                    var context = $"Object '{obj.Name}' property '{property.Name}'";
                    if (CheckName(property.Name, TypeGenDiagnosticCodes.BackendInvalidProperty, $"Object '{obj.Name}' property name", propertyLocation) &&
                        duplicateNames.Contains(property.Name))
                    {
                        Error(TypeGenDiagnosticCodes.BackendInvalidProperty, $"{context} is declared more than once.", propertyLocation);
                    }

                    const PropertyFlags knownFlags = PropertyFlags.Required | PropertyFlags.ReadOnly | PropertyFlags.WriteOnly |
                        PropertyFlags.DeployTimeConstant | PropertyFlags.Identifier;
                    if ((property.Flags & ~knownFlags) != 0)
                    {
                        Error(TypeGenDiagnosticCodes.BackendInvalidPropertyFlags, $"{context} has unknown property flag bits.", propertyLocation);
                    }

                    if (property.Versioning is not { } versioning)
                    {
                        Error(TypeGenDiagnosticCodes.BackendInvalidGraph, $"{context} versioning must be initialized.", propertyLocation);
                    }
                    else
                    {
                        ValidateAvailability(versioning.Availability, propertyLocation, context);
                        if (versioning.NameChangedFrom is not null || versioning.TypeChangedFrom is not null || versioning.RequiredChangedFrom is not null)
                        {
                            Error(TypeGenDiagnosticCodes.BackendUnsupportedSemantic, $"{context} contains unsupported version transitions.", propertyLocation);
                        }
                    }

                    pending.Enqueue((property.Type, propertyLocation, context));
                }
            }

            if (obj.AdditionalProperties is { } additional)
            {
                pending.Enqueue((additional, location, $"Object '{obj.Name}' additional properties"));
            }
        }

        private void ValidateUnion(UnionTypeDefinition union, SourceLocation? location)
        {
            if (union.Elements.IsDefaultOrEmpty)
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidGraph, "Union elements must be initialized and nonempty.", location);
                return;
            }

            foreach (var element in union.Elements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (element is null)
                {
                    Error(TypeGenDiagnosticCodes.BackendInvalidGraph, "Union elements cannot contain null.", location);
                    continue;
                }

                var elementLocation = element.SourceLocation ?? location;
                ValidateAvailability(element.Availability, elementLocation, "Union element");
                pending.Enqueue((element.Type, elementLocation, "Union element"));
            }
        }

        private void ValidateAvailability(VersionAvailability? availability, SourceLocation? location, string context)
        {
            if (availability is null)
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidGraph, $"{context} availability must be initialized.", location);
            }
            else if (availability.AddedAt is not null || availability.RemovedAt is not null)
            {
                Error(TypeGenDiagnosticCodes.BackendUnsupportedSemantic, $"{context} contains unsupported lifecycle availability.", location);
            }
        }

        private void ValidateLengths(long? min, long? max, SourceLocation? location, string context)
        {
            if (min < 0 || max < 0 || (min.HasValue && max.HasValue && min > max))
            {
                Error(TypeGenDiagnosticCodes.BackendInvalidConstraint, $"{context} length bounds must be nonnegative and ordered.", location);
            }
        }

        private bool CheckName(string? name, string code, string context, SourceLocation? location)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Error(code, $"{context} cannot be null, empty, or whitespace.", location);
                return false;
            }

            return true;
        }

        private void Error(string code, string message, SourceLocation? location)
        {
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Add(new(code, TypeGenDiagnosticSeverity.Error, message, location));
        }
    }
}
