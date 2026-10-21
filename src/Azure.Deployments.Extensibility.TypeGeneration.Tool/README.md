# Type Generation Tool

This is the command-line entry point for the
[Type Generation SDK](../Azure.Deployments.Extensibility.TypeGeneration.Sdk/README.md).
It evaluates a C# project, obtains a Roslyn compilation, calls the SDK, and prints the ETSM
as a readable tree. It does not implement a second type analyzer.

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

The extension references the `.Sdk` project/package and uses its existing
`Azure.Deployments.Extensibility.TypeGeneration.Attributes` namespace. It does not reference
the Tool or the old Local Extension SDK. The
[Contoso sample](../../sample/Azure.Deployments.Extensibility.TypeGeneration.Sample/README.md)
provides a ready-to-run project, a complete output snapshot, two input-change experiments,
and a real duplicate-configuration error example.

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

Properties are evaluated for the selected framework in Debug configuration, not obtained by
reading raw XML or reflecting over an extension DLL. Multiple target frameworks require
`--framework`; an undeclared framework is an error. Workspaces loads the project graph with
its normal reference-framework selection. The tool then selects the root project by its
evaluated output path, rather than blindly taking the first framework. Framework output paths
must be distinct. All frameworks that Workspaces loads need valid restore assets and SDK support;
the tool does not ignore failures in unselected project variants.

Restore the project and its references beforehand. The tool neither restores automatically
nor builds/executes the extension's output assembly. Analysis does not call Emit or read that
assembly. MSBuild still evaluates imports and runs design-time targets: **only analyze trusted
projects**, since MSBuild projects/tasks can execute code.

## Output and failures

- `--format tree` is the only format and the default.
- Standard output contains only the completed tree (or `--help`).
- Standard error contains compiler, SDK, and workspace diagnostics. Source diagnostics include
  one-based line/column when the document is available, otherwise the original character span.
- Stable local IDs and `ref #N` represent shared/cyclic graph nodes. IDs are display-only,
  not canonical type-package references.
- This phase prints resource version labels without projecting separate versioned resources.
- There is no `index.json`/`types.json` generation, packaging, publishing, or extension execution.

| Exit | Meaning |
|---|---|
| 0 | Analysis and tree output succeeded, or help was requested |
| 1 | Project loading, compilation, semantic analysis, inspection, or I/O failed |
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
not build these fixture projects. A throwing module initializer and absence-of-DLL checks
guard against accidentally depending on their runtime output. Tests clean up their own
temporary project directories.
