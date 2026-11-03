# Contoso type generation sample

This sample turns C# model definitions into a readable Extension Type Semantic Model (ETSM)
or a canonical type package.
It is not an extension service: there is no entry point, runtime handler, or deployment.
The Tool analyzes the source project without building or executing its DLL.

## What the sample shows

| Input | Result |
|---|---|
| `BicepExtensionName=Contoso`, `BicepExtensionVersion=1.2` | One extension snapshot |
| `Repository` with two API-version labels | One ETSM resource; two package registrations sharing one Body |
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

MSBuild may create ordinary design-time files under `obj`. Inspection does not produce a
sample DLL or a type package. Do not run `dotnet run` on the sample project itself.
Only analyze trusted projects: MSBuild imports and design-time tasks can execute code even
though the sample's runtime assembly is not executed.

## Generate a type package

After the same preparation, run this command from the repository root:

```powershell
dotnet run --project 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj' --configuration Debug --framework net10.0 --no-build --no-restore -- generate --project 'sample\Azure.Deployments.Extensibility.TypeGeneration.Sample\Azure.Deployments.Extensibility.TypeGeneration.Sample.csproj' --output '.\artifacts\contoso\types.tgz'
```

Expected: exit **0**, empty stderr, and one stdout summary containing `resourceNames=1`,
`concreteResources=2`, `publication=written` and `validation=internal-json-and-archive`.
The relative output path is resolved from the current working directory. Repeating the command
without changing inputs reports `publication=unchanged` and preserves the archive's timestamp.

The archive contains exactly two root-level files, `index.json` and `types.json`; the Tool does
not write loose JSON sidecars or a sample DLL. The resource index contains
`Repository@2026-08-01` and `Repository@2026-09-01`. They point to distinct ResourceType nodes
with the same Body reference. The package also preserves configuration, inherited flags,
nullable types, the enum literals, array/map types and the recursive settings reference.
Function-index format fields are absent. Node indexes are package-local, not inspection tree IDs.

This is the inner type archive, not a deployable extension runtime or a complete outer Bicep
extension envelope. No external Validator, deployment or registry publishing runs in this command.
See the [Tool README](../../src/Azure.Deployments.Extensibility.TypeGeneration.Tool/README.md)
for configuration selection, source generators, diagnostics and the publication commit boundary.

## Try a changed input

Use a disposable copy for experiments, keeping its SDK project reference valid.
The automated tests perform both of the following changes in isolated copies.

Add this property inside `Repository`, before `Settings`:

```csharp
[TypeProperty("Archived")]
public bool Archived { get; set; }
```

Run inspection or generation again. The complete model and tree now include an `archived`
Boolean property; both package registrations see that property through their shared Body.
No new registration or special-case generator code is needed.

Separately, change only the two API-version strings to `"preview"` and `"stable"`.
Run again. Inspection changes the resource's labels; generation changes its concrete registration
keys. The semantic Body stays the same. Projection does not evaluate per-version lifecycle rules.

Adding a third label, or removing one of the original two, has the same rule: the model still
has one resource with the unchanged body, while the package has one entry for each declared
label. An empty version list produces one unversioned `Repository` entry, not a synthetic
default version. The integration tests check replacement, addition, removal and the empty list
separately, including repeated generation of each changed input.

If a later edit introduces a compiler or authoring error, generation exits nonzero with no
success summary. A previously generated package keeps its bytes and timestamp; a new output
is not created. Do not mistake the retained old archive for a successful result from the failing
input.

## Run the complete isolated demo

Use PowerShell 7.4 or newer and the repository-selected .NET SDK. The demo has been verified on
Windows. From the repository root, prepare the Tool (not the sample runtime) and choose a new
workspace:

```powershell
dotnet restore 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj'
dotnet build 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj' --configuration Release --framework net10.0 --no-restore
$workspace = Join-Path ([System.IO.Path]::GetTempPath()) ('ContosoTypeGenDemo-' + [guid]::NewGuid().ToString('N'))
pwsh -NoProfile -File 'sample\Azure.Deployments.Extensibility.TypeGeneration.Sample\Invoke-TypeGenerationDemo.ps1' -Workspace $workspace -Configuration Release
Get-Content -LiteralPath (Join-Path $workspace 'summary.json')
```

[Invoke-TypeGenerationDemo.ps1](Invoke-TypeGenerationDemo.ps1) refuses an existing workspace,
copies only the sample's inputs, fixes its SDK reference, and explicitly restores that new copy.
It does not edit the repository's models, build the consumer DLL, or clean other directories.
It then:

1. Generates a package and extracts its two JSON files using the platform Tar/GZip reader.
2. Checks both registrations, their shared Body and inherited Identifier flags.
3. Repeats generation and checks unchanged bytes and timestamp.
4. Adds `Archived` only to the copy and verifies a Boolean property in both registrations.
5. Introduces a compiler error and requires exit 1, empty stdout, TG1101, and unchanged last-valid
   archive bytes/timestamp.

The script exits 0 only when all checks, including the expected failing generation, succeed.
The copied project intentionally ends with an invalid `Failure.cs`. The top-level `types.tgz`
is the **last valid package**, not a successful output from that invalid source. The workspace
retains `baseline/` and `changed/` archives/JSON, per-command stdout/stderr logs and `summary.json`;
choose another new workspace to repeat the complete demo.

The demo is not an external Validator. Separate acceptance with Bicep CLI **0.48.1
(`cec7951cf6`)** compiled both versions of the original and changed CLI archives, wrapped
unchanged in a local extension envelope. Wrong Identifier types produced BCP036 errors; Boolean
property expressions assigned to string outputs produced BCP033 errors. Ordinary resource-property
assignment mismatches can instead be BCP036 **warnings**, and optional properties can accept null:
these are consumer policies, not evidence that a package field was dropped. See the pinned
[resource-property validation](https://github.com/Azure/bicep/blob/cec7951cf694f1067eb17b5b062209fb66c710a9/src/Bicep.Core/TypeSystem/TypeValidator.cs#L1149)
and [implicit-null flag mapping](https://github.com/Azure/bicep/blob/cec7951cf694f1067eb17b5b062209fb66c710a9/src/Bicep.Core/TypeSystem/Providers/Extensibility/ExtensionResourceTypeFactory.cs#L167).
No deployment or registry publishing is part of this verification.

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
dotnet test 'src\Azure.Deployments.Extensibility.TypeGeneration.Tests\Azure.Deployments.Extensibility.TypeGeneration.Tests.csproj' --configuration Debug --filter 'FullyQualifiedName~ContosoSampleTests|FullyQualifiedName~SampleCliTests|FullyQualifiedName~PackageEndToEndTests|FullyQualifiedName~SourceGeneratorTests.Generate_'
```

Tests compare the complete typed model with an independently built expected graph. They also
check stdout/stderr, process exit codes, exact new files, preserved input hashes, source spans,
repeatability across processes/directories, and the input changes above. Package tests invoke
fresh CLI processes and read Tar/GZip/JSON directly; their semantic expectations are not generated
by the production SDK. Existing ETSM/tree snapshots remain separate acceptance checks.

The tests also verify that referenced projects' independent resource/configuration declarations
are not discovered as roots, while referenced base/property types still bind. Selected build
configuration/framework and source-generated properties reach the actual package. Generator
errors and invalid generated properties preserve a real previously generated archive.

Snapshots normalize only line endings to LF and, for diagnostics, the exact temporary project
directory prefix to `<project>/`. Type names, flags, descriptions, IDs, ordering, and source
line/column numbers are not removed or rewritten. Tree output contains no machine-specific
source paths. Do not update a snapshot just to hide a semantic or diagnostic change.

This sample demonstrates source analysis, tree inspection and type-package generation, not
automatic build integration or deployment.
