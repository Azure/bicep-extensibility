# Type Generation SDK

This project owns the C# authoring attributes for build-time Bicep extension type generation.
The current implementation provides the attributes, the Extension Type Semantic Model (ETSM),
the frontend, resource/configuration registration checks, property metadata, and structural
C#-to-ETSM mapping. The authoring example below can be analyzed into a complete typed model.
The generated graph is validated before a successful result is returned. The companion
[Tool](../Azure.Deployments.Extensibility.TypeGeneration.Tool/README.md) loads real projects
and prints this graph. Type-file generation is not implemented yet.

The SDK project, assembly, and package name is `Azure.Deployments.Extensibility.TypeGeneration.Sdk`.
Public namespaces remain `Azure.Deployments.Extensibility.TypeGeneration` and its subnamespaces.
The SDK does not depend on the Tool, MSBuild, or Roslyn Workspaces.

## Authoring

Reference `Azure.Deployments.Extensibility.TypeGeneration.Sdk` and use:

```csharp
using Azure.Bicep.Types.Concrete;
using Azure.Deployments.Extensibility.TypeGeneration.Attributes;

[ResourceType("Repository", ApiVersions = new[] { "2026-08-01", "2026-09-01" })]
public sealed class Repository
{
    [TypeProperty("Repository name",
        ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public required string Name { get; init; }

    [TypeProperty("Repository description")]
    public string? Description { get; init; }
}

[BicepConfiguration]
public sealed class Configuration
{
    [TypeProperty("Authentication token", ObjectTypePropertyFlags.Required, isSecure: true)]
    public required string Token { get; init; }
}
```

`ResourceTypeAttribute` preserves the existing constructor with an optional single API version:

```csharp
[ResourceType("Repository", "2026-08-01")]
public sealed class LegacyVersionExample;
```

The frontend interprets that single version as a one-element version list.
A non-null legacy `ApiVersion` together with a non-empty `ApiVersions` list is invalid input;
the frontend reports that conflict. Those interpretation and validation steps are not
implemented by the attributes themselves.

`ApiVersions` defaults to an empty array and preserves the supplied order. `FullName` retains
its legacy behavior: it uses `Name` and the single `ApiVersion`, not the new version array.
It is not a replacement for frontend version processing.

`TypePropertyAttribute` retains its description, `ObjectTypePropertyFlags`, and `isSecure`
parameters. A null description becomes an empty string. C# `required` and the Bicep
`Required` flag remain separate. The frontend preserves supported sensitive-string
and collection-element metadata; `isSecure` is not encryption.

`BicepConfigurationAttribute` applies to classes, allows only one occurrence on a class,
and is not inherited by derived classes. Enforcing at most one configuration class in an
extension belongs to the frontend, not to this marker.

## Frontend API

`Roslyn.ITypeGenerationFrontend` declares the synchronous
`Build(Compilation, ExtensionBuildMetadata, CancellationToken)` entry point.
`Roslyn.TypeGenerationFrontend` implements this interface. It validates inputs before
discovering effectively public types in the target compilation, including nested and partial
classes. Referenced assemblies are not scanned for registrations. Attributes are read from
Roslyn symbols, without emitting or loading the user's assembly.

Resource markers retain their inherited behavior; a direct marker overrides an inherited one.
Configuration markers are not inherited. At most one visible configuration class is allowed.
Resource names are ordinal and unique even for disjoint version lists. Version labels keep
their original order and spelling, without date parsing or trimming. Null arrays, null/blank
labels, duplicate labels, and conflicting legacy/array version inputs are errors.

Successful registrations are ordered by resource name. Resources use `ResourceScope.All`
for both readable and writable scopes, with empty function lists. Configuration and fallback
are null when absent; preview and deprecated are false.

Roots must be non-static class/record object schemas without open type parameters. Collection
roots are not resource/configuration objects. Reachable properties can use the supported types
listed below, including constructed generic classes and inherited or positional record properties.
Indexers remain errors (TG1403).

For example, a Roslyn host with a resolved compilation can analyze the authoring model:

```csharp
using Azure.Deployments.Extensibility.TypeGeneration;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;

var frontend = new TypeGenerationFrontend();
var result = frontend.Build(compilation, new ExtensionBuildMetadata("Contoso", "1.0.0"));
if (result.Succeeded)
{
    var model = result.Model!;
    // Pass model to inspection or a future type-package backend.
}
```

`ExtensionBuildMetadata` carries the resolved name, version, and optional singleton setting.
Names and versions must be non-empty and not whitespace-only; no SemVer parser or implicit
trimming is applied. Project-file loading and boolean text parsing belong to the future host.

`TypeGenerationResult` contains a model and immutable diagnostics. Success requires a model
and no errors. Its constructor rejects an uninitialized diagnostic array, null diagnostic
entries, or a partial model accompanied by errors. Warnings and informational diagnostics
do not make a complete result fail.

| Code | Meaning |
|---|---|
| TG1001 | Missing extension name |
| TG1002 | Missing extension version |
| TG1101 | C# compiler error or warning; the message retains the original CS identifier |
| TG1102 | Missing, ambiguous, or incorrectly sourced authoring SDK types |
| TG1201 | Null or blank resource name |
| TG1202 | Null version array, invalid version label, or repeated label |
| TG1203 | Both legacy and array version inputs supplied |
| TG1204 | Repeated public resource name |
| TG1205 | Multiple configuration classes |
| TG1301 | Invalid resource/configuration object root |
| TG1401 | Multiple effective properties have the same exported name |
| TG1402 | Unknown TypeProperty flag bits |
| TG1403 | Indexer cannot be represented as an object property |
| TG1501 | Unsupported structural type or array shape; empty enum |
| TG1502 | Integer range cannot be represented by signed long bounds |
| TG1503 | Dictionary key is not string |
| TG1504 | Conflicting collection element or dictionary value types |
| TG1505 | Warning: nullable-oblivious reference retains its legacy non-null shape |
| TG1506 | Type expansion exceeds the supported depth |
| TG1601 | Generated graph violates the current Phase 1 normalized-model contract |

Compiler errors prevent further model analysis. Diagnostics retain original UTF-16 source
spans and syntax-tree file paths, including when a `#line` directive changes display locations.
Input validation supports cancellation and keeps its symbol and diagnostic state per call.

### Property selection and metadata

The internal property collector is wired into the frontend's validation. It includes
unannotated public instance properties, inherited properties, and positional record properties,
but not static/non-public properties, fields, or explicit interface implementations.
Properties follow legacy reflection order: derived declarations first, then remaining base
members. Constructed generic bases retain substituted types and nullability.
For partial declarations, source paths are ordered ordinally and properties retain their
order within each file, so reordering compilation inputs does not reorder the schema.
Metadata-only members retain their declared metadata order.

Only the first character of a CLR property name is lowercased invariantly (`URL` becomes `uRL`).
`JsonPropertyName` and `JsonIgnore` do not change type generation. With no `TypeProperty`
attribute, the description is null; an explicit null description becomes an empty string.
All existing flag bits are mapped explicitly. C# `required`, setter accessibility, and
nullability never infer Bicep flags. `isSecure` is passed to structural type mapping.

Ordinary overrides occur once and inherit the nearest `TypeProperty` metadata unless replaced
by a direct marker. `new` members with the same CLR property signature hide the base member,
without inheriting its metadata. Non-public hiding follows reflection's accessibility rules;
a static member does not hide an instance property. Signatures treat `dynamic` and `object`
as the same runtime type, including array and generic arguments.

Unlike the old generator's last-write-wins dictionary, two effective properties with the same
exported name are errors. This includes same-name hiding or covariant overrides where reflection
retains different return types. Indexers are deliberately rejected rather than exported as
ordinary `item` properties. These checks never return a partial successful model.

### Structural type mapping

| C# shape | ETSM |
|---|---|
| `string`, `bool` | String, Boolean |
| `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long` | Integer with the exact CLR min/max |
| `T?` / nullable reference occurrence | Union containing the underlying type and Null |
| One-dimensional vector arrays and a uniquely identifiable `IEnumerable<T>` | Array |
| String-key `IDictionary<TKey,TValue>` / `IReadOnlyDictionary<TKey,TValue>` and implementations | Object with no fixed properties and a typed AdditionalProperties edge |
| Enum | String-literal union using raw names in unsigned numeric order; aliases retained |
| Reachable class/record | Object containing the effective properties |

Supported collections include `List`, collection/list/read-only interfaces, `HashSet`, `Queue`,
`Stack`, `LinkedList`, and immutable collection implementations exposing the generic enumerable
contract. Dictionary recognition takes precedence over enumeration; invalid keys or ambiguous
interfaces are not silently converted to arrays. Maps have deterministic synthetic names derived
from the first reachable semantic path, not random IDs.

Nullable annotation is preserved at each level: `List<string?>?` has both a nullable outer
array and nullable string items. Value-type `Nullable<T>` is handled separately from reference
annotations. A reference with `NullableAnnotation.None` (for example, `#nullable disable`)
retains its legacy non-null shape and emits TG1505 at the use site. This is a compatibility
policy, not a claim that the author declared the reference non-null.

`isSecure` marks strings, including nullable strings and string elements reached through
supported nested collections. It does not mark numeric types, entire objects, or dictionary
values; an object's own property markers are still honored. Secure and plain occurrences
cannot overwrite one another through the type cache.

`ulong`, native integers, `char`, floating-point/decimal types, arbitrary `object`/`dynamic`,
non-collection interfaces/structs, delegates, pointers, function pointers, multidimensional
arrays and non-generic collections are not supported. Empty enums have no named value to
export and are rejected. Enum serialization names are ignored, as in the legacy generator.
Unknown types are never replaced by Any or empty objects.

The mapper first validates the reachable type graph, then materializes the ETSM only when
there are no errors. Closed generic arguments and their nullability are part of symbol identity.
Per-call construction callbacks connect ordinary self/mutual cycles without truncating edges.
Continuous first-time type expansion is limited to 128 shapes; TG1506 rejects deeper or
infinitely expanding generic inputs. A repeated active type is a back-edge, not another expansion.
Source-located diagnostics are returned with Model=null on errors, never a partial model.

### Final graph validation and regression baseline

The frontend validates the materialized graph before exposing it. The internal
[PhaseOneModelValidator](Roslyn/Validation/PhaseOneModelValidator.cs) checks resource/configuration
object roots, unique names and version labels, scopes, required edges and collections,
property flags, enum/nullable unions, bounds, and the absence of future lifecycle/function/
fallback data. It is specific to this frontend's current output, not a universal validator
for every future ETSM authoring surface.

Validation uses an iterative queue and reference-identity visited set, so shared or cyclic
graphs are not truncated or traversed indefinitely. Terminal literal/null occurrences still
have their context checked even when a node is shared. Repeated unsupported scalar/collection
types report each independent property use site; errors inside a shared object are reported
at the actual property declaration once.

The [Contoso authoring fixture](../Azure.Deployments.Extensibility.TypeGeneration.Tests/Support/ContosoAuthoringFixture.cs)
contains multi-file C# input, extension metadata, and a separately constructed expected ETSM.
It reuses the signed-model fixture, adjusted to the verified derived-before-inherited property
order. This is a typed semantic golden reusable by the future backend, not a canonical JSON
snapshot. Regression tests cover source-tree permutations, precise locations, shared/cyclic
graphs, nullable specialization, secure/plain property order, repeated and concurrent builds,
and isolation across compilations. Source locations are tested independently and excluded from
cycle-safe semantic comparison.

## Semantic model

The `Azure.Deployments.Extensibility.TypeGeneration.Semantics` namespace contains the complete
ETSM data structures: the extension snapshot, resource registrations, type graph, properties,
version metadata, functions, projected resource/function records, and source locations.
The models do not contain Roslyn objects, authoring attributes, or serialized type references.
Defining projected records does not implement API-version projection.

For example, this constructs a resource model directly, without analyzing C#:

```csharp
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

var body = new ObjectTypeDefinition
{
    Name = "Repository",
    Properties =
    [
        new PropertyDefinition
        {
            Name = "name",
            Type = new StringTypeDefinition(),
            Flags = PropertyFlags.Required | PropertyFlags.Identifier,
        },
    ],
};

var resource = new ResourceTypeDefinition
{
    Name = "Repository",
    ApiVersions = ["2026-08-01", "2026-09-01"],
    Body = body,
    ReadableScopes = ResourceScope.All,
    WritableScopes = ResourceScope.All,
    ResourceFunctions = [],
};
```

The records store values; they do not discover resources, apply authoring rules, or validate
the complete schema. Those checks belong to the frontend. Required properties and init-only
setters preserve the model's public shape.

Type nodes may be shared or cyclic. Internal constructors for composite types use initialization
callbacks to connect edges to nodes that are still being constructed. These callbacks may
reference a node, but must not inspect its unfinished children or publish an incomplete graph.
The node's public properties remain init-only; there are no mutable public setters, reference-ID
nodes, or reflection-based field updates.

Do not use generated record equality, hashing, or recursive `ToString()` as graph traversal or
semantic comparison. Graph operations must track visited nodes or node pairs by reference.
`SourceLocation` uses UTF-16 offsets and is diagnostic information, not type semantics.
The tests use a cycle-safe comparer that ignores source locations.

## Migrating from Local Extension attributes

- Replace the authoring reference to `Azure.Bicep.Local.Extension` with this SDK.
- Replace `using Bicep.Local.Extension.Types.Attributes;` with
  `using Azure.Deployments.Extensibility.TypeGeneration.Attributes;`.
- Keep `using Azure.Bicep.Types.Concrete;` for `ObjectTypePropertyFlags`. This SDK references
  the `Azure.Bicep.Types` package for those flags, not the old Local Extension SDK.
- Resource and property attribute constructor arguments retain their existing shape.
- The old runtime does not automatically recognize these new attribute types. This project
  does not change or remove that runtime; any runtime migration is a separate task.

This SDK and its tests do not require a checkout or build of the `Azure/bicep` repository.

## Source and license

The following definitions were adapted from `Azure/bicep` commit
`713f13d7c0ed353aae4ed08bc458137330c4d683`:

- [ResourceTypeAttribute](https://github.com/Azure/bicep/blob/713f13d7c0ed353aae4ed08bc458137330c4d683/src/Bicep.Local.Extension/Types/Attributes/ResourceTypeAttribute.cs)
- [TypePropertyAttribute](https://github.com/Azure/bicep/blob/713f13d7c0ed353aae4ed08bc458137330c4d683/src/Bicep.Local.Extension/Types/Attributes/TypePropertyAttribute.cs)

Their Microsoft copyright and MIT notices are preserved. See the repository [LICENSE](../../LICENSE).
The namespace, build-time documentation, and `ApiVersions` property are the migration changes.
`BicepConfigurationAttribute` is new. Further authoring changes are maintained in this repository.

## Build and test

From the repository root:

```powershell
dotnet build .\src\Azure.Deployments.Extensibility.TypeGeneration.Tests\Azure.Deployments.Extensibility.TypeGeneration.Tests.csproj --configuration Debug
dotnet test .\src\Azure.Deployments.Extensibility.TypeGeneration.Tests\Azure.Deployments.Extensibility.TypeGeneration.Tests.csproj --configuration Debug --no-build --filter "Category=Unit"
```
