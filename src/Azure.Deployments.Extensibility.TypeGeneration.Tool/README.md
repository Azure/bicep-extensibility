# Type Generation Tool

This is the command-line entry point for the
[Type Generation SDK](../Azure.Deployments.Extensibility.TypeGeneration.Sdk/README.md).
It evaluates a C# project, obtains a Roslyn compilation and calls the SDK. It can inspect the
ETSM as a readable tree or generate and safely publish a type archive. It does not implement
a second type analyzer.

The solution groups `.Sdk`, `.Tool`, and `.Tests` under `TypeGeneration`.
Only this tool depends on MSBuild and Roslyn Workspaces.

## Run from the repository

Use the .NET SDK selected by the repository's `global.json`. From the repository root:

```powershell
# Replace this path with your extension's SDK-style C# project.
dotnet restore 'C:\work\Example\Example.csproj'
dotnet run --project 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj' --framework net10.0 -- --project 'C:\work\Example\Example.csproj' --format tree
```

The first `--framework` selects the tool's runtime. For a multi-target extension, pass its
framework after the `--` separator as well:

```powershell
dotnet run --project 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj' --framework net10.0 -- --project 'C:\work\Example\Example.csproj' --framework net10.0 --name Contoso --version 1.2 --singleton false
```

The existing no-verb form remains an alias for `inspect`. To inspect the input project's
Release compilation explicitly:

```powershell
dotnet run --project 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj' --framework net10.0 -- inspect --project 'C:\work\Example\Example.csproj' --configuration Release
```

The `--configuration` after `--` selects the input project's compilation, independently of
the configuration used to build the Tool itself. Its default is Debug; custom nonblank
configuration names are accepted without silently selecting Debug instead.

`--format` is inspect-only and `--output` is generate-only. Use `inspect --help` /
`generate --help` for command-specific help; bare `--help` / `-h` remains supported.

## Generate a type archive

```powershell
dotnet run --project 'src\Azure.Deployments.Extensibility.TypeGeneration.Tool\Azure.Deployments.Extensibility.TypeGeneration.Tool.csproj' --framework net10.0 -- generate --project 'C:\work\Example\Example.csproj' --configuration Release --output '.\artifacts\types.tgz'
```

The command reuses one Host/Frontend analysis, calls the SDK's complete package generator, and
formats backend diagnostics using the captured source text. The archive is published only after
internal JSON, semantic, reference and archive checks succeed. This does not run an external
Validator, deploy an extension or publish to a registry.

Output paths are relative to the current working directory, not the input project's directory.
The Tool rejects directory destinations and collisions with the input project or captured source
files. Existing directory links are resolved for comparison/publication; linked/reparse-point
output files are rejected. On Windows, ambiguous trailing-dot/space components and invalid file
name characters are rejected rather than relying on Win32 alias normalization.

Publishing creates a unique staging file in the destination directory, writes and flushes the
complete archive, then atomically moves it over the destination without deleting the old file
first. Identical bytes leave the target content and timestamp unchanged. Failures or cancellation
before the commit preserve an existing target; cleanup only touches this call's owned staging
file. Cleanup failures are reported with the staging path. Newly created empty parent directories
may remain after an unsuccessful publication.

The final rename is the commit boundary. Cancellation after a successful commit is not reported
as if publication were undone. If writing the success summary fails after publication, TGH1014
explicitly reports that the package is already written/unchanged; it does not claim rollback.

The extension references the `.Sdk` project/package and uses its existing
`Azure.Deployments.Extensibility.TypeGeneration.Attributes` namespace. It does not reference
the Tool or the old Local Extension SDK. The
[Contoso sample](../../sample/Azure.Deployments.Extensibility.TypeGeneration.Sample/README.md)
provides a ready-to-run inspection/generation project, a complete tree snapshot, package
expectations, an executable isolated success/change/failure demo, and a real
duplicate-configuration error example.

The demo checks retained bytes and timestamps after an expected failed generation; it does not
mistake an existing output file for success. Build automation should likewise use the exit code
and diagnostics, not file existence alone. TGH1014 is an explicit post-publication summary failure.
An identical valid package intentionally retains its timestamp. Automatic MSBuild integration
and global-tool installation are not delivered by this command.

## Project settings

These values may come from the project or imported files, including conditional properties:

```xml
<PropertyGroup>
  <BicepExtensionName>Contoso</BicepExtensionName>
  <BicepExtensionVersion>1.2</BicepExtensionVersion>
  <BicepExtensionIsSingleton>false</BicepExtensionIsSingleton>
</PropertyGroup>
```

Name and version are required unless overridden on the command line. The singleton property
defaults to false only when absent/empty. Otherwise it must contain Boolean text (`true` or
`false`, case-insensitive); numbers and `yes`/`no` are errors. An explicit CLI override wins,
including `--singleton false` over a true or invalid project value.

Properties are evaluated for the selected framework and configuration (Debug by default), not obtained by
reading raw XML or reflecting over an extension DLL. Multiple target frameworks require
`--framework`; an undeclared framework is an error. Workspaces loads the project graph with
its normal reference-framework selection. The tool then selects the root project by its
evaluated output path, rather than blindly taking the first framework. Framework output paths
must be distinct. All frameworks that Workspaces loads need valid restore assets and SDK support;
the tool does not ignore failures in unselected project variants.

Restore the project and its references beforehand. The tool neither restores automatically
nor builds/executes the extension's output assembly. Analysis does not call Emit or read that
assembly. MSBuild still evaluates imports and runs design-time targets: **only analyze trusted
projects**, since MSBuild projects/tasks and source generators can execute code.

### Source generators

Resource/configuration root discovery is limited to declarations in the input project.
Independent roots declared in project references are not merged into the package. Referenced
property/base types, including generated helper types, can still participate in the root's
reachable type graph. This is not cross-project root discovery.

C# source generators supplied through Analyzer references participate in analysis. Generated
partial declarations and reachable helper types are included in the compilation passed to the
SDK; the consumer project's output DLL and emitted generated-source files are not required.
Project-referenced generator assemblies must already be built for the input project's selected
configuration, in addition to restoring its dependencies. Building the generator is different
from building or executing the extension assembly.

The Tool uses the public Roslyn generator driver once per reachable project, in dependency order,
with that project's parse options, analyzer configuration, and additional files. It does not
first run generators through Workspaces and then rerun them to recover diagnostics.
The frontend still consumes an ordinary `Compilation`; its type-mapping rules are unchanged.

Generator errors, execution exceptions, and unresolved/unloadable analyzer references fail
analysis with TGH1009 and no successful model. Generator warnings are reported while allowing a
complete model to succeed, respecting the project's warning settings. A generation exception
remains fatal even if the compiler's corresponding failure warning is suppressed. Generated
source locations use virtual paths; source line/column information is shown when available.

The Host retains immutable source text from the actual root compilation and reachable compilation
references, including generated trees. Later backend diagnostics reuse this snapshot after the
Workspace is disposed; no second project load, generator execution or disk reread is required.
If a path has conflicting source texts, is unknown, or has an invalid span, formatting preserves
the original span instead of inventing a line/column. Physical UTF-16 offsets are used, not `#line`
remapping.

The compatibility tests cover an incremental generator adding properties/helper types,
additional files and compiler options, generated types in a referenced project, error/warning
handling, and single execution. This does not claim equivalence with every custom build target
or diagnostic analyzer run by `dotnet build`.

## Output and failures

- For inspect, `--format tree` is the only format and the default.
- Inspection stdout contains only the completed tree (or help). Generation stdout contains one
  summary only after successful publication: identity, distinct resource-name count, concrete
  resource count, resolved output path, written/unchanged status and internal validation status.
- Standard error contains compiler, SDK, and workspace diagnostics. Source diagnostics include
  one-based line/column when the document is available, otherwise the original character span.
- Stable local IDs and `ref #N` represent shared/cyclic graph nodes. IDs are display-only,
  not canonical type-package references.
- Inspection prints resource version labels without projecting separate versioned resources.
- Generation writes one `types.tgz` containing `index.json` and `types.json`; it does not write
  JSON sidecars or execute the extension runtime.

| Exit | Meaning |
|---|---|
| 0 | Inspection, generation/publication (including unchanged output), or help succeeded |
| 1 | Loading, analysis, generation, publication, cleanup, summary output or other I/O failed |
| 2 | Invalid command-line arguments |
| 130 | Cancellation, including Ctrl+C |

| Diagnostic | Meaning |
|---|---|
| TGH0001 | Invalid CLI arguments |
| TGH0002 | Cancelled |
| TGH1001 | MSBuild registration failed |
| TGH1002 | Invalid, missing, unreadable, or unevaluable project |
| TGH1003 | Missing, undeclared, or ambiguous framework selection |
| TGH1004 | Workspace loading failure/warning |
| TGH1005 | Restore assets missing |
| TGH1006 | Invalid singleton project setting |
| TGH1007 | Unsupported tree output |
| TGH1008 | I/O failure |
| TGH1009 | Source-generator diagnostics or analyzer/generator loading failure |
| TGH1010 | Blank build configuration supplied directly to the Host |
| TGH1011 | Invalid output destination or input/output collision |
| TGH1012 | Output-path access, staging write, comparison or atomic publication failed |
| TGH1013 | Owned staging-file cleanup failed; original operation context is retained |
| TGH1014 | Publication completed, but the success summary could not be written |

SDK diagnostics retain their `TGxxxx` codes. A compiler warning can accompany successful
output, but a diagnostic classified by Workspaces as `Failure` is fatal, even if the original
MSBuild event was a warning. Failures never return a partial model as success.

## Tests

From the repository root:

```powershell
dotnet test 'src\Azure.Deployments.Extensibility.TypeGeneration.Tests\Azure.Deployments.Extensibility.TypeGeneration.Tests.csproj' --filter 'Category=Unit|Category=Integration'
```

Integration tests create isolated real projects using the shared Contoso authoring fixture,
restore them, and compare the result with the independently constructed typed ETSM. They do
not build the projects being inspected. A throwing module initializer and absence-of-DLL checks
guard against accidentally depending on their runtime output. Tests clean up their own
temporary project directories.

Source-generator compatibility additionally builds a separate control copy whose handwritten
code references generated members. Disabling generation makes that control fail compilation;
the Tool then analyzes a different, unbuilt copy and checks the complete expected ETSM. Only
the test generator's assembly is a prerequisite for that analysis.
