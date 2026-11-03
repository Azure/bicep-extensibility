# Type Generation SDK

This project owns the C# authoring attributes for build-time Bicep extension type generation.
The current implementation provides the attributes, the Extension Type Semantic Model (ETSM),
the frontend, resource/configuration registration checks, property metadata, and structural
C#-to-ETSM mapping. The authoring example below can be analyzed into a complete typed model.
The generated graph is validated before a successful result is returned. The companion
[Tool](../Azure.Deployments.Extensibility.TypeGeneration.Tool/README.md) loads real projects
and prints this graph. The separate backend generates a verified in-memory type package;
the Tool's `generate` command can publish that package safely to an output file.

The SDK project, assembly, and package name is `Azure.Deployments.Extensibility.TypeGeneration.Sdk`.
Public namespaces remain `Azure.Deployments.Extensibility.TypeGeneration` and its subnamespaces.
The SDK does not depend on the Tool, MSBuild, or Roslyn Workspaces.

For a complete runnable example, see the
[Contoso authoring sample](../../sample/Azure.Deployments.Extensibility.TypeGeneration.Sample/README.md).
It includes nested collections, a map, an enum, nullable properties, a recursive type,
and a real invalid-input example. It needs restore, not a prebuilt sample DLL.

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

For example, a Roslyn host with a resolved compilation can analyze the authoring model.
Here `reportDiagnostic` is the host's diagnostic-reporting callback:

```csharp
using Azure.Deployments.Extensibility.TypeGeneration;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;

var frontend = new TypeGenerationFrontend();
var result = frontend.Build(compilation, new ExtensionBuildMetadata("Contoso", "1.0.0"));
foreach (var diagnostic in result.Diagnostics)
{
    reportDiagnostic(diagnostic);
}

if (!result.Succeeded)
{
    return;
}

var model = result.Model!;
// Pass the validated model to the next stage, without re-reading authoring attributes.
```

`ExtensionBuildMetadata` carries the resolved name, version, and optional singleton setting.
Names and versions must be non-empty and not whitespace-only; no SemVer parser or implicit
trimming is applied. Project-file loading and boolean text parsing belong to the companion
[Tool](../Azure.Deployments.Extensibility.TypeGeneration.Tool/README.md), not the SDK.

`TypeGenerationResult` contains a model and immutable diagnostics. Success requires a model
and no errors. Its constructor rejects an uninitialized diagnostic array, null diagnostic
entries, or a partial model accompanied by errors. Warnings and informational diagnostics
do not make a complete result fail.

The model is a graph, not necessarily a tree: shared and cyclic nodes retain reference identity.
Consumers should use reference-based visited sets rather than recursive record equality or
`ToString()`. Source locations support diagnostics but are not semantic identity. Resource
version labels remain unprojected; downstream consumers must not treat each label as an already
materialized resource. The frontend stops at ETSM; the separate package generator performs
projection, lowering, serialization, validation and archiving.

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

## Backend input validation and package contracts

`Backend.Validation.TypeGenerationModelValidator.Validate(model, cancellationToken)` validates
an ETSM without loading a project, using Roslyn, projecting versions, or writing output.
It returns immutable diagnostics, with an empty array for valid supported input. Null arguments
throw argument exceptions; cancellation throws `OperationCanceledException`.

The validator supports the frontend's complete current output, shared/cyclic graphs, basic scalar
constraints, and disjoint API-version definitions with the same resource name. This does not relax
the frontend's authoring restrictions. Validation is iterative and does not mutate the graph or
normalize names/version labels. Diagnostics use the nearest available source location and stable
ordering. Unknown types or invalid graph edges are errors, never `Any` fallbacks.

Lifecycle metadata, discriminated objects, resource functions, fallback resources, and non-default
preview/deprecated settings are explicitly unsupported. Extension Resource packages omit the
`resourceFunctions` and `namespaceFunctions` format fields, even when empty. Ordinary business
properties with those names remain valid; the fixed ETSM contracts are not removed or reshaped.

`Backend.TypeGenerationPackage` and `Backend.TypeGenerationPackageResult` define the immutable
payload/result contract for later generation. Package construction is internal and requires all
three nonempty buffers: index JSON, type JSON, and archive bytes. This is a payload-shape check,
not JSON/archive validation; the generation pipeline validates content before constructing it.
An error result cannot expose a package; warnings and informational diagnostics can accompany a
complete package. The public package generator below returns these contracts after all checks;
the Tool's `generate` command uses the same API.

| Code | Backend validation meaning |
|---|---|
| TG2001 | Missing extension name or version |
| TG2002 | Invalid resource name |
| TG2003 | Uninitialized, blank, null, or repeated API-version label |
| TG2004 | Overlapping registrations or mixed versioned/unversioned definitions |
| TG2005 | Resource/configuration root is not object-like |
| TG2006 | Invalid graph structure, required collections, or metadata |
| TG2007 | Invalid or duplicate property name |
| TG2008 | Unknown property flag bits |
| TG2009 | Unknown scope bits |
| TG2010 | Invalid scalar or length bounds |
| TG2011 | Unsupported semantic metadata |
| TG2012 | Unsupported type node |
| TG2201 | Multiple concrete resources encode to the same public resource key |
| TG2301 | JSON serialization failed; neither output buffer is returned |
| TG2401 | Invalid JSON document, UTF-8 or string encoding |
| TG2402 | Invalid JSON shape, missing/duplicate/unknown/null member or unsupported kind |
| TG2403 | Invalid reference placement, file, index, shape or target kind |
| TG2404 | Invalid JSON value, bounds, flags/scopes or resource entry identity |
| TG2405 | Serialized semantics differ from the expected projection |
| TG2501 | Archive creation failed |
| TG2502 | Archive framing, layout, metadata or content verification failed |

### API-version projection

The internal `Backend.Projection.TypeGenerationProjectionService.Project` stage validates the
entire ETSM using the same backend validator before producing any projected resources. Failure
returns the original diagnostics and no projected model, even if other resources were valid.
Null arguments and cancellation retain the validator's exception contract.

An empty API-version list produces one `ProjectedResourceTypeDefinition` with `ApiVersion = null`;
otherwise each declared label produces one resource in its original order. Resource-definition
order is also preserved. Names and API versions remain separate: no `Name@ApiVersion` keys,
JSON references, file names, or type-file indices are introduced here.

Each resource keeps its scopes and source location. The stage returns extension name, version,
singleton setting, configuration and source location alongside the projected resources in an
internal result container, without adding a public ETSM model. Bodies and configuration retain
their original reference identity, including shared and recursive nodes. No per-version deep
copy is needed because lifecycle metadata is explicitly rejected before projection.

Same-name disjoint version sets are accepted; overlapping sets, mixed versioned/unversioned
definitions and unsupported semantics fail with the existing backend diagnostics. This does
not change the frontend's more restrictive registration rules. Internal projected function
arrays are initialized but empty; they do not imply emitting function indexes in JSON.
The projection stage does not itself generate a package or implement a CLI command.

### Type graph lowering

The internal `Backend.Lowering.TypeGenerationLowerer.Lower` stage consumes the projected extension.
An internal projected-input entry point reuses the backend validator's graph checks, so hand-built
projected inputs cannot bypass root, constraint, scope/flag or unsupported-semantic validation.
It does not revisit C# or evaluate lifecycle metadata. Invalid input returns diagnostics and no
partial target graph.

Lowering uses the actual `Azure.Bicep.Types` models and `TypeFactory` references. It registers
semantic nodes by reference identity in iterative depth-first order: configuration first, then
resource bodies by final public key, with ordinal property traversal and original union-element
order. It creates all target nodes, appends resource nodes by public key, then hydrates references
once before returning. Shared nodes and cycles remain shared/cyclic; equal-looking independent
nodes are not merged. Input graphs are never mutated, and each call owns its caches and factory.

Public keys are the unversioned name or `Name@ApiVersion`. Duplicate encoded keys, including
collisions between different name/version pairs containing `@`, fail with TG2201 at both sources.
Scope and property flags are mapped explicitly; nullable unions, map values, descriptions, bounds,
patterns and sensitive markers are preserved. Legacy preview/deprecated settings are passed as
null, not their unsafe library defaults.

The internal result contains a real `TypeIndex` and an immutable array of `TypeBase` objects,
with in-memory references targeting the planned single `types.json`. The library requires
non-null function-index collections, so these are empty internally. This is not permission to
emit them: the later writer must omit both format fields under the Extension Resource contract.
This stage does not itself write JSON or an archive.

### Canonical JSON writing

The internal `Backend.Serialization.TypeGenerationJsonWriter.Write` stage turns a lowered model
into immutable `index.json` and `types.json` buffers. It reuses the type library's reference
converters and source-generated metadata, with narrowly scoped metadata modifiers for the
Extension Resource format. No second type model or general JSON serializer is introduced.

Output uses UTF-8 without a BOM, two-space indentation and LF, including a final LF. Required
false/zero values and empty required objects remain present. Absent optional values are omitted,
and unused function indexes, embedded functions and legacy preview/deprecated settings are not
emitted. Modifiers only affect the corresponding library types: ordinary business properties,
resource names and string values with these names remain untouched.

Nonempty function indexes/embedded functions, enabled legacy settings or fallback resources fail before omission;
they are never silently discarded. Recognized serializer/reference failures produce TG2301 with
file context and no partial buffers. Null arguments and cancellation remain exceptions.
Each call owns its options and reference factory; library defaults and other callers are not
modified. Deterministic numbering and collection order come from Lowering.

The serialized result is a candidate for the next validation stage, not a validated archive.

### Independent package validation and JSON generation

The internal `Backend.Validation.TypeGenerationPackageValidator.Validate` reads the actual
index/type byte buffers using `JsonDocument`, independently of the Bicep serializer and
TypeFactory. It checks this generator's fixed-file output subset: exact fields and kinds,
duplicate members, optional-null omission, required defaults, numeric bounds, flags/scopes,
reference syntax/placement/range and target kinds. Every reference is checked, including
references in cycles; the validator does not recursively expand the type graph.

Function indexes are forbidden format members even when empty. Same-named ordinary properties
remain valid. This is not a general legacy/multi-file Bicep package validator; extension settings
are required by this producer's contract. JSON whitespace is not semantic, and validation never
rewrites or normalizes the supplied files.

An optional expected projected model adds an independent comparison of extension metadata,
resource/configuration entries and effective type-graph semantics. It uses reference-identity
node/index pairs to terminate cycles while allowing equivalent graphs with different sharing
topologies. Field/value changes cannot pass merely because the JSON is structurally valid.
Diagnostics include the JSON file/pointer; semantic differences retain available expected
source locations. Invalid input returns no validated-package marker.

`Backend.TypeGenerationJsonGenerator.Generate` connects Projection, Lowering, canonical writing
and validation in memory. It always supplies the expected projection, propagates stage diagnostics,
and returns the original immutable files only after every stage succeeds. Null arguments and
cancellation remain exceptions. The internal validated-JSON handoff is ready for the archive stage;
no placeholder archive bytes are created.

### Complete package generation

`Backend.TypeGenerationPackageGenerator.Generate(model, cancellationToken)` is the public,
synchronous ETSM-to-package entry point. It runs the JSON pipeline, creates a Tar/Gzip archive
from the validated files, and verifies the archive before returning `TypeGenerationPackageResult`.
Successful results contain index JSON, type JSON and actual `types.tgz` bytes; failures expose
diagnostics and no partial package. The generator is stateless and does not load a project,
write files, publish to a registry or start an extension runtime.

```csharp
using Azure.Deployments.Extensibility.TypeGeneration.Backend;

var result = new TypeGenerationPackageGenerator().Generate(model, cancellationToken);
if (result.Succeeded)
{
    var archiveBytes = result.Package!.ArchiveBytes;
}
```

The archive contains exactly two Ustar regular files, `index.json` then `types.json`, at the root.
Entries use Unix epoch timestamps, uid/gid zero, empty owner/group names and mode 0644. The
writer buffers the complete Tar before compressing it with `GZipStream` at Optimal level.
Repeated and concurrent runs are byte-identical on the tested runtime/platform.

The internal archive verifier is intentionally limited to this writer's own output profile,
not an arbitrary archive importer. It bounds decompression by the expected validated content,
checks the exact layout/metadata/padding and file bytes using `TarReader`, and independently
validates the extracted JSON. It also recompresses the decoded Tar to check current-runtime
canonical framing, because `GZipStream` can otherwise accept trailing input. Alternative
compression encodings may be valid for consumers but are not accepted by this own-output check.
No cross-runtime or cross-OS byte-level guarantee is claimed without verification.

The type archive is the inner `types.tgz` payload. A local Bicep extension may need a separate
outer extension envelope; that is not added by this generator. Output publication is handled
by the [Tool's generate command](../Azure.Deployments.Extensibility.TypeGeneration.Tool/README.md),
not by the SDK.

### Integration boundaries

The backend's public input remains `ExtensionTypeSemanticModel`, not a Roslyn compilation,
project path or serialized type graph. Resource discovery, metadata precedence and source-text
capture belong upstream. A future cross-project discovery flow can supply the same validated
ETSM contract without replacing projection, serialization, validation or packaging. Current
project analysis still discovers resource/configuration roots only in the selected input project;
reachable referenced types are usable, but independent referenced roots are not merged.

Build automation should call the same Tool `generate` entry point rather than duplicate its
analysis or publication logic. It must handle nonzero exits and retained old outputs; file
existence alone is not success, and byte-identical output deliberately does not get a new
timestamp. Automatic MSBuild targets and global-tool installation are separate work.

The [Contoso demo](../../sample/Azure.Deployments.Extensibility.TypeGeneration.Sample/README.md#run-the-complete-isolated-demo)
exercises the complete CLI flow in a new workspace without executing a consumer DLL.
Independent acceptance used Bicep CLI 0.48.1 (`cec7951cf6`) with the exact emitted archive inside
a verification-only outer envelope. This does not claim an external Validator ran in the SDK,
that every Bicep resource-property mismatch is fatal, or that deployment/registry publishing
has been implemented.

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
