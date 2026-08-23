[CmdletBinding()]
param(
    [ValidateSet("x64", "arm64")]
    [string] $Architecture = "x64",

    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string] $Version = "1.2.0.0",

    [switch] $Store
)

$ErrorActionPreference = "Stop"
$env:AVALONIA_TELEMETRY_OPTOUT = "1"
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repoRoot "src\Openza.Reader.Avalonia\Openza.Reader.Avalonia.csproj"
$manifestTemplatePath = Join-Path $repoRoot "src\Openza.Reader.Avalonia\Package.Windows.appxmanifest"
$assetSource = Join-Path $repoRoot "src\Openza.Reader\Assets"
$runtimeIdentifier = "win-$Architecture"
$packageKind = if ($Store) { "Store" } else { "Dev" }
$artifactRoot = Join-Path $repoRoot "artifacts\windows\$runtimeIdentifier\$($packageKind.ToLowerInvariant())"
$publishRoot = Join-Path $artifactRoot "publish"
$stageRoot = Join-Path $artifactRoot "package"
$packagePath = Join-Path $artifactRoot "Openza.Reader-$Version-$Architecture-$packageKind.msix"

$resolvedRepoRoot = [IO.Path]::GetFullPath($repoRoot)
$resolvedArtifactRoot = [IO.Path]::GetFullPath($artifactRoot)
if (-not $resolvedArtifactRoot.StartsWith($resolvedRepoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Packaging output must remain inside the repository artifacts directory."
}

if (Test-Path -LiteralPath $artifactRoot) {
    Remove-Item -LiteralPath $artifactRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null

dotnet publish $projectPath `
    -c Release `
    -f net10.0-windows10.0.19041.0 `
    -r $runtimeIdentifier `
    --self-contained true `
    --no-restore `
    -p:PublishSingleFile=false `
    -o $publishRoot
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item -Path (Join-Path $publishRoot "*") -Destination $stageRoot -Recurse -Force
$packageAssets = Join-Path $stageRoot "Assets"
New-Item -ItemType Directory -Path $packageAssets -Force | Out-Null
foreach ($assetName in @("StoreLogo.png", "Square44x44Logo.png", "Square150x150Logo.png", "Wide310x150Logo.png")) {
    Copy-Item -LiteralPath (Join-Path $assetSource $assetName) -Destination (Join-Path $packageAssets $assetName) -Force
}

$identity = if ($Store) { "Openza.OpenzaReader" } else { "Openza.OpenzaReader.Avalonia.Dev" }
$displayName = if ($Store) { "Openza Reader" } else { "Openza Reader Dev" }
$manifest = Get-Content -LiteralPath $manifestTemplatePath -Raw
$manifest = $manifest.Replace("__IDENTITY__", $identity)
$manifest = $manifest.Replace("__DISPLAY_NAME__", $displayName)
$manifest = $manifest.Replace("__VERSION__", $Version)
$manifest = $manifest.Replace("__ARCHITECTURE__", $Architecture)
Set-Content -LiteralPath (Join-Path $stageRoot "AppxManifest.xml") -Value $manifest -Encoding utf8NoBOM

$windowsKitsBin = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
$makeAppx = Get-ChildItem -LiteralPath $windowsKitsBin -Directory |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName "x64\makeappx.exe" } |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1
if (-not $makeAppx) {
    throw "MakeAppx.exe was not found. Install the Windows 10/11 SDK."
}

& $makeAppx pack /d $stageRoot /p $packagePath /o
if ($LASTEXITCODE -ne 0) {
    throw "MakeAppx failed with exit code $LASTEXITCODE."
}

Write-Host "Created unsigned $packageKind package: $packagePath"
if ($Store) {
    Write-Host "Upload this unsigned package to Partner Center; Microsoft Store applies the production signature."
} else {
    Write-Host "The development package uses a separate identity and must be signed or registered as a loose package for local testing."
}
