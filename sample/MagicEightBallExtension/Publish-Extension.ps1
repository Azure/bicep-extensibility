#Requires -Version 7.0
<#
.SYNOPSIS
    Builds MagicEightBall as a self-contained linux-x64 binary and publishes it as a Bicep extension.

.DESCRIPTION
    Publishes the project with `dotnet publish`, then pushes the binary and the hand-written types
    in ./types with `bicep publish-extension --contract v2`, which marks the binary layer as an Extension API
    Contract V2 (HTTP) binary. Omit -Target to only build the binary.

.EXAMPLE
    # From this directory, with the Bicep repo cloned next to this repo
    ./Publish-Extension.ps1 -Target br:myregistry.azurecr.io/bicep/extensions/magiceightball:2.0.0 -BicepPath ../../../bicep/src/Bicep.Cli/bin/Debug/net10.0/bicep
#>
[CmdletBinding()]
param(
    # A `br:` registry reference. When omitted, only the binary is built.
    [string] $Target,

    [string] $BicepPath = 'bicep',

    [string] $Configuration = 'Release',

    # Outside the project directory so MSBuild doesn't glob previous output back into the project.
    [string] $OutputPath = (Join-Path $PSScriptRoot '..' '..' 'artifacts' 'MagicEightBallExtension'),

    [switch] $Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$project = Join-Path $PSScriptRoot 'MagicEightBallExtension.csproj'
$indexFile = Join-Path $PSScriptRoot 'types' 'index.json'
$publishDir = Join-Path $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath) 'linux-x64'
$binary = Join-Path $publishDir 'MagicEightBallExtension'

dotnet publish $project --configuration $Configuration --runtime linux-x64 --self-contained `
    -p:PublishSingleFile=true --output $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$hash = (Get-FileHash $binary -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Binary: $binary"
Write-Host "Size:   $((Get-Item $binary).Length) bytes"
Write-Host "Digest: sha256:$hash"

if (-not $Target) {
    return
}

$publishArgs = @('publish-extension', $indexFile, '--contract', 'v2', '--bin-linux-x64', $binary, '--target', $Target)
if ($Force) {
    $publishArgs += '--force'
}

Write-Host "Publishing to $Target..."
& $BicepPath @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "bicep publish-extension failed with exit code $LASTEXITCODE."
}

Write-Host "Published $Target"
