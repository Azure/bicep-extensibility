# Contoso type generation sample

This sample turns C# model definitions into a readable Extension Type Semantic Model (ETSM).
It is not an extension service: there is no entry point, runtime handler, or deployment.
The Tool analyzes the source project without building or executing its DLL.

## What the sample shows

| Input | Result |
|---|---|
| `BicepExtensionName=Contoso`, `BicepExtensionVersion=1.2` | One extension snapshot |
| `Repository` with two API-version labels | One resource with two labels, not two projected resources |
| Inherited `RepositoryIdentifiers.Name` | Required and Identifier flags |
| Nullable `Description` and read-only `Url` | Nullable string types with their declared flags |
| `ContosoConfiguration.Organization` | Extension configuration |
| `RepositorySettings.Topics` | Array of strings |
| `RepositorySettings.Labels` | String-key map with nullable string values |
| `RepositorySettings.Visibility` | Union of the literal names `Private` and `Public` |
| Nullable `Settings` and `Parent` | A nullable object and a cycle in the type graph |

`Parent` demonstrates a recursive **type**. The tool never creates a runtime object graph.
Local IDs and `ref #N` in the output let the tree display shared types and cycles without
expanding them forever.

## Prepare and run

Use the .NET SDK selected by the repository's `global.json`. Run these commands from the
repository root:

```powershell
dotnet restore 'sample\Azure.Deployments.Extensibility.TypeGeneration.Sample\Azure.Deployments.Extensibility.TypeGeneration.Sample.csproj'
dotnet build 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj' --configuration Debug
```

Restore prepares the sample's dependencies. Build builds the **Tool and SDK**, not the sample.
After preparation, this one command prints the complete tree:

```powershell
dotnet run --project 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj' --configuration Debug --framework net10.0 --no-build --no-restore -- --project 'sample\Azure.Deployments.Extensibility.TypeGeneration.Sample\Azure.Deployments.Extensibility.TypeGeneration.Sample.csproj' --format tree
```

Expected: exit code **0**, the tree on stdout, and empty stderr. The complete expected output
is in [Contoso.tree.txt](../../src/Azure.Deployments.Extensibility.TypeGeneration.Tests/Fixtures/Snapshots/Contoso.tree.txt).
It starts with:

```text
extension "Contoso" version="1.2" singleton=false
  preview=false deprecated=false
  configuration: #1 object "ContosoConfiguration" sensitive=false
```

The tree includes `versions=["2026-08-01", "2026-09-01"]`, the inherited `name` property,
and `Parent` pointing back to `ref #6`. Node IDs are for inspection, not a serialized type
package format.

MSBuild may create ordinary design-time files under `obj`. The tool does not produce a
sample DLL, `index.json`, or `types.json`. Do not run `dotnet run` on the sample project itself.
Only analyze trusted projects: MSBuild imports and design-time tasks can execute code even
though the sample's runtime assembly is not executed.

## Try a changed input

Use a disposable copy for experiments, keeping its SDK project reference valid.
The automated tests perform both of the following changes in isolated copies.

Add this property inside `Repository`, before `Settings`:

```csharp
[TypeProperty("Archived")]
public bool Archived { get; set; }
```

Run the Tool again. The complete model and tree should now include an `archived` Boolean
property. No new registration or special-case generator code is needed.

Separately, change only the two API-version strings to `"preview"` and `"stable"`.
Run again. The resource's labels change, but its body stays the same. This stage has not
implemented per-version projection.

Adding a third label, or removing one of the original two, has the same rule: the model still
has one resource with the unchanged body. The integration tests check replacement, addition,
and removal separately.

## See an error with a source location

The [DuplicateConfiguration fixture](../../src/Azure.Deployments.Extensibility.TypeGeneration.Tests/Fixtures/DuplicateConfiguration/DuplicateConfiguration.csproj)
contains two configuration classes. It is valid C#, but invalid authoring for one extension.
It is not compiled into the test assembly and is not a solution build entry.

```powershell
dotnet restore 'src\Azure.Deployments.Extensibility.TypeGeneration.Tests\Fixtures\DuplicateConfiguration\DuplicateConfiguration.csproj'
dotnet run --project 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj' --configuration Debug --framework net10.0 --no-build --no-restore -- --project 'src\Azure.Deployments.Extensibility.TypeGeneration.Tests\Fixtures\DuplicateConfiguration\DuplicateConfiguration.csproj' --format tree
```

Expected: exit code **1**, empty stdout, and two `error TG1205` diagnostics on stderr,
pointing to `Configuration.cs(8,2)` and `Configuration.cs(11,2)`. No partial model is presented
as success. The full expected diagnostic text is in
[DuplicateConfiguration.stderr.txt](../../src/Azure.Deployments.Extensibility.TypeGeneration.Tests/Fixtures/Snapshots/DuplicateConfiguration.stderr.txt);
`<project>` stands for the actual fixture directory.

## Reference the SDK from another project

This sample uses a project reference to
`Azure.Deployments.Extensibility.TypeGeneration.Sdk`, with no dependency on the old
`Azure.Bicep.Local.Extension` SDK or a neighboring Bicep checkout.

When moving authoring to this SDK:

1. Reference the `.Sdk` project (or its package when available).
2. Replace the old `Bicep.Local.Extension.Types.Attributes` using with
   `Azure.Deployments.Extensibility.TypeGeneration.Attributes`.
3. Continue using `Azure.Bicep.Types.Concrete.ObjectTypePropertyFlags` for flags.
4. Supply extension name/version as project properties or Tool overrides.

The old and new attribute classes are different types. This does not migrate runtime handlers
or make an existing runtime automatically understand the new attributes.
See the [SDK README](../../src/Azure.Deployments.Extensibility.TypeGeneration.Sdk/README.md)
for supported C# shapes and explicit limits, including unsupported floating-point types,
non-string map keys, and open generic roots. C# `required` does not by itself set the Bicep
Required flag.

## Tests and snapshot rules

```powershell
dotnet test 'src\Azure.Deployments.Extensibility.TypeGeneration.Tests\Azure.Deployments.Extensibility.TypeGeneration.Tests.csproj' --configuration Debug --filter 'FullyQualifiedName~ContosoSampleTests|FullyQualifiedName~SampleCliTests'
```

Tests compare the complete typed model with an independently built expected graph. They also
check stdout/stderr, process exit codes, exact new files, preserved input hashes, source spans,
repeatability across processes/directories, and the two input changes above.

Snapshots normalize only line endings to LF and, for diagnostics, the exact temporary project
directory prefix to `<project>/`. Type names, flags, descriptions, IDs, ordering, and source
line/column numbers are not removed or rewritten. Tree output contains no machine-specific
source paths. Do not update a snapshot just to hide a semantic or diagnostic change.

This sample demonstrates source analysis and tree inspection, not canonical type-package generation.
