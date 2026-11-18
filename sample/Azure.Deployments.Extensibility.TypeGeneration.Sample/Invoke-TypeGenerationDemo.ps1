#Requires -Version 7.4
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Workspace,
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
Set-StrictMode -Version Latest

$repository = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$workspacePath = [System.IO.Path]::GetFullPath($Workspace)
$tool = Join-Path $repository "src\Azure.Deployments.Extensibility.TypeGeneration.Tool\bin\$Configuration\net10.0\Azure.Deployments.Extensibility.TypeGeneration.Tool.dll"
if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) {
    throw "Build the Tool in $Configuration first; see the sample README. Missing: $tool"
}
if (Test-Path -LiteralPath $workspacePath) { throw "Choose a new workspace; refusing to overwrite '$workspacePath'." }
New-Item -ItemType Directory -Path $workspacePath | Out-Null
$projectDirectory = Join-Path $workspacePath 'project'
New-Item -ItemType Directory -Path $projectDirectory | Out-Null
$projectPath = Join-Path $projectDirectory 'ContosoDemo.csproj'
$target = Join-Path $workspacePath 'types.tgz'

function Write-Step([string] $Message) {
    Write-Output "[$(Get-Date -Format o)] $Message"
}

function Invoke-Generate([string] $Label, [int] $ExpectedExit = 0) {
    $stdoutPath = Join-Path $workspacePath "$Label.stdout.log"
    $stderrPath = Join-Path $workspacePath "$Label.stderr.log"
    & dotnet $tool generate --project $projectPath --configuration $Configuration --output $target 1> $stdoutPath 2> $stderrPath
    $code = $LASTEXITCODE
    $stdout = [System.IO.File]::ReadAllText($stdoutPath)
    $stderr = [System.IO.File]::ReadAllText($stderrPath)
    if ($code -ne $ExpectedExit) { throw "$Label exited $code (expected $ExpectedExit).`n$stdout`n$stderr" }
    if ($ExpectedExit -eq 0 -and ($stderr -ne '' -or $stdout -notmatch 'resourceNames=1 concreteResources=2')) {
        throw "Unexpected successful generate output.`n$stdout`n$stderr"
    }
    return [pscustomobject]@{ ExitCode = $code; Output = $stdout; Error = $stderr }
}

function Resolve-TypeReference($Reference, [object[]] $Types, [switch] $CrossFile) {
    $prefix = if ($CrossFile) { 'types.json#/' } else { '#/' }
    if ($Reference.Count -ne 1 -or -not $Reference.Contains('$ref') -or
        $Reference['$ref'] -notmatch "^$([regex]::Escape($prefix))([0-9]+)$") {
        throw 'Invalid package reference.'
    }
    $id = 0
    if (-not [int]::TryParse($Matches[1], [ref] $id) -or $id -ge $Types.Count) { throw 'Reference is outside the type array.' }
    return $Types[$id]
}

function Read-Package([string] $Destination, [bool] $ExpectArchived) {
    New-Item -ItemType Directory -Path $Destination | Out-Null
    [System.IO.File]::Copy($target, (Join-Path $Destination 'types.tgz'))
    $file = [System.IO.File]::OpenRead($target)
    try {
        $gzip = [System.IO.Compression.GZipStream]::new($file, [System.IO.Compression.CompressionMode]::Decompress)
        try {
            $reader = [System.Formats.Tar.TarReader]::new($gzip)
            try {
                foreach ($name in @('index.json', 'types.json')) {
                    $entry = $reader.GetNextEntry()
                    if ($null -eq $entry -or $entry.Name -cne $name -or
                        $entry.EntryType -ne [System.Formats.Tar.TarEntryType]::RegularFile -or
                        $entry.Format -ne [System.Formats.Tar.TarEntryFormat]::Ustar) {
                        throw "Expected a Ustar regular file named $name."
                    }
                    $data = [System.IO.MemoryStream]::new()
                    try {
                        $entry.DataStream.CopyTo($data)
                        [System.IO.File]::WriteAllBytes((Join-Path $Destination $name), $data.ToArray())
                    }
                    finally { $data.Dispose() }
                }
                if ($null -ne $reader.GetNextEntry()) { throw 'Unexpected extra archive entry.' }
            }
            finally { $reader.Dispose() }
        }
        finally { $gzip.Dispose() }
    }
    finally { $file.Dispose() }
    $index = Get-Content -LiteralPath (Join-Path $Destination 'index.json') -Raw | ConvertFrom-Json -AsHashtable
    $types = Get-Content -LiteralPath (Join-Path $Destination 'types.json') -Raw | ConvertFrom-Json -AsHashtable -NoEnumerate
    if ($index.Contains('resourceFunctions') -or $index.Contains('namespaceFunctions') -or
        $index['settings']['name'] -cne 'Contoso' -or $index['settings']['version'] -cne '1.2') {
        throw 'Unexpected package settings or function-index fields.'
    }
    $keys = @($index['resources'].Keys | Sort-Object)
    if (($keys -join '|') -cne 'Repository@2026-08-01|Repository@2026-09-01') { throw 'Expected both declared versions.' }
    $bodyReference = $null
    foreach ($key in $keys) {
        $resource = Resolve-TypeReference $index['resources'][$key] $types -CrossFile
        if ($resource['$type'] -cne 'ResourceType' -or $resource['name'] -cne $key) { throw 'Incorrect resource target.' }
        $reference = $resource['body']['$ref']
        if ($null -ne $bodyReference -and $reference -cne $bodyReference) { throw 'Versions do not share their Body.' }
        $bodyReference = $reference
        $body = Resolve-TypeReference $resource['body'] $types
        if ($body['$type'] -cne 'ObjectType' -or $body['properties']['name']['flags'] -ne 17 -or
            $body['properties'].Contains('archived') -ne $ExpectArchived) { throw 'Incorrect Body properties or Identifier flags.' }
        if ($ExpectArchived) {
            $archived = Resolve-TypeReference $body['properties']['archived']['type'] $types
            if ($archived['$type'] -cne 'BooleanType' -or $body['properties']['archived']['flags'] -ne 0) {
                throw 'Archived was not emitted as the expected Boolean property.'
            }
        }
    }
    return [pscustomobject]@{ Resources = $keys; SharedBody = $bodyReference; Archived = $ExpectArchived }
}

Write-Step "Preparing an isolated copy at $projectDirectory."
$project = [System.Xml.Linq.XDocument]::Load((Join-Path $PSScriptRoot 'Azure.Deployments.Extensibility.TypeGeneration.Sample.csproj'))
foreach ($reference in $project.Descendants('ProjectReference')) {
    $reference.Attribute('Include').Value = [System.IO.Path]::GetFullPath($reference.Attribute('Include').Value, $PSScriptRoot)
}
$project.Save($projectPath)
Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File | Copy-Item -Destination $projectDirectory
[System.IO.File]::WriteAllText((Join-Path $projectDirectory 'Directory.Build.props'), '<Project />')
[System.IO.File]::WriteAllText((Join-Path $projectDirectory 'Directory.Build.targets'), '<Project />')
Copy-Item -LiteralPath (Join-Path $repository 'global.json') -Destination $projectDirectory
& dotnet restore $projectPath --no-dependencies --nologo "-p:Configuration=$Configuration" -p:NuGetAudit=false `
    1> (Join-Path $workspacePath 'restore.stdout.log') 2> (Join-Path $workspacePath 'restore.stderr.log')
if ($LASTEXITCODE -ne 0) { throw "Restore failed; see restore logs in $workspacePath." }

Write-Step 'Generating and extracting the original package.'
$initial = Invoke-Generate 'initial'
Write-Output $initial.Output
if ($initial.Output -notmatch 'publication=written') { throw 'Initial package was not published.' }
$original = Read-Package (Join-Path $workspacePath 'baseline') $false
$originalHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
[System.IO.File]::SetLastWriteTimeUtc($target, [DateTime]::new(2020, 1, 2, 3, 4, 5, [DateTimeKind]::Utc))
$timestamp = [System.IO.File]::GetLastWriteTimeUtc($target)
$repeat = Invoke-Generate 'repeat'
if ($repeat.Output -notmatch 'publication=unchanged' -or
    (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -cne $originalHash -or
    [System.IO.File]::GetLastWriteTimeUtc($target) -ne $timestamp) { throw 'Unchanged publication rewrote the package.' }
Write-Output $repeat.Output

Write-Step 'Adding Archived to the copied model and checking both versions.'
$sourcePath = Join-Path $projectDirectory 'Repository.cs'
$source = [System.IO.File]::ReadAllText($sourcePath)
$marker = '    [TypeProperty("Repository settings")]'
if ([regex]::Matches($source, [regex]::Escape($marker)).Count -ne 1) { throw 'Sample marker is no longer unique.' }
$property = "    [TypeProperty(`"Archived`")]`n    public bool Archived { get; set; }`n`n"
[System.IO.File]::WriteAllText($sourcePath, $source.Replace($marker, $property + $marker, [StringComparison]::Ordinal))
$updated = Invoke-Generate 'updated'
Write-Output $updated.Output
if ($updated.Output -notmatch 'publication=written') { throw 'Updated package was not published.' }
$changed = Read-Package (Join-Path $workspacePath 'changed') $true
$changedHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
if ($changedHash -ceq $originalHash) { throw 'The property change did not change the archive.' }

Write-Step 'Introducing a compiler error; the last valid archive must be retained, not reported as a new success.'
[System.IO.File]::SetLastWriteTimeUtc($target, [DateTime]::new(2020, 1, 2, 3, 4, 5, [DateTimeKind]::Utc))
$timestamp = [System.IO.File]::GetLastWriteTimeUtc($target)
[System.IO.File]::WriteAllText((Join-Path $projectDirectory 'Failure.cs'),
    'public class Failure { public MissingType Value { get; set; } }')
$failure = Invoke-Generate 'expected-failure' 1
Write-Output $failure.Error
if ($failure.Output -ne '' -or $failure.Error -notmatch 'error TG1101' -or
    (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -cne $changedHash -or
    [System.IO.File]::GetLastWriteTimeUtc($target) -ne $timestamp) { throw 'Failed input did not preserve the old package correctly.' }
if (Get-ChildItem -LiteralPath $workspacePath -Filter '*.tmp' -Recurse -File) { throw 'A temporary publication file was left behind.' }
if (Get-ChildItem -LiteralPath $projectDirectory -Recurse -File | Where-Object Extension -In '.dll', '.pdb') {
    throw 'The demo unexpectedly built a consumer assembly.'
}
$summary = [ordered]@{
    configuration = $Configuration
    baseline = $original
    changed = $changed
    baselineSha256 = $originalHash
    changedSha256 = $changedHash
    expectedFailureExitCode = $failure.ExitCode
    lastValidArchivePreserved = $true
    copiedProjectIntentionallyInvalid = $true
    externalValidationRun = $false
}
[System.IO.File]::WriteAllText((Join-Path $workspacePath 'summary.json'), ($summary | ConvertTo-Json -Depth 6))
Write-Step "PASS: expected failure preserved the last valid archive. The copied project intentionally contains Failure.cs. Evidence: $workspacePath"
exit 0
