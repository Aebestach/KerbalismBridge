#Requires -Version 5.1
<#
.SYNOPSIS
  Build Release (optional) and pack four KSP mod zips for GitHub Release.

.DESCRIPTION
  Each zip contains:
    GameData/zKerbalismXXX/
    Extras/...          (SystemHeat and FFT only, when present)
    LICENSE
    README.md           (from docs/mods/, monorepo header stripped)
    CHANGELOG.md        (monorepo header + this mod's section from root CHANGELOG)

  Output: dist/KerbalismSystemHeat.v<Version>.zip (Version is manual, e.g. 1.0.0-beta.1)

.EXAMPLE
  .\scripts\package-release.ps1 -Version 1.0.0

.EXAMPLE
  .\scripts\package-release.ps1 -Version 1.0.0-beta.1 -SkipBuild

.EXAMPLE
  .\scripts\package-release.ps1 -Version v1.0.0-beta.1 -SkipBuild
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Version,

    [string] $OutputDir = "dist",

    [switch] $SkipBuild
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
Set-Location $RepoRoot

# Manual release label: 1.0.0, 1.0.0-beta.1, v1.0.0-beta.1, etc.
$VersionTag = $Version.Trim()
if ($VersionTag.StartsWith('v')) { $VersionTag = $VersionTag.Substring(1) }
if ([string]::IsNullOrWhiteSpace($VersionTag)) {
    throw "Version must not be empty (example: 1.0.0-beta.1)"
}
$VersionLabel = "v$VersionTag"

$Mods = @(
    @{
        Id           = "zKerbalismSystemHeat"
        ReleaseName  = "KerbalismSystemHeat"
        GameDataDir  = "GameData\zKerbalismSystemHeat"
        DllName      = "zKerbalismSystemHeat.dll"
        ReadmeSource = "docs\mods\zKerbalismSystemHeat.md"
        ExtrasDir    = "Extras\zKerbalismSystemHeat"
    },
    @{
        Id           = "zKerbalismFFT"
        ReleaseName  = "KerbalismFFT"
        GameDataDir  = "GameData\zKerbalismFFT"
        DllName      = "zKerbalismFFT.dll"
        ReadmeSource = "docs\mods\zKerbalismFFT.md"
        ExtrasDir    = "Extras\zKerbalismFFT"
    },
    @{
        Id           = "zKerbalismNFE"
        ReleaseName  = "KerbalismNFE"
        GameDataDir  = "GameData\zKerbalismNFE"
        DllName      = "zKerbalismNFE.dll"
        ReadmeSource = "docs\mods\zKerbalismNFE.md"
        ExtrasDir    = $null
    },
    @{
        Id           = "zKerbalismDynamicRadiation"
        ReleaseName  = "KerbalismDynamicRadiation"
        GameDataDir  = "GameData\zKerbalismDynamicRadiation"
        DllName      = "zKerbalismDynamicRadiation.dll"
        ReadmeSource = "docs\mods\zKerbalismDynamicRadiation.md"
        ExtrasDir    = $null
    }
)

function Find-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $path = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" 2>$null | Select-Object -First 1
        if ($path -and (Test-Path $path)) { return $path }
    }
    throw "MSBuild not found. Build from Visual Studio or install VS Build Tools."
}

function Get-ModChangelog {
    param(
        [string] $ModId,
        [string] $SourcePath
    )

    $lines = Get-Content -LiteralPath $SourcePath
    $header = @(
        "# Changelog - $ModId"
        ""
        "**Version:** $VersionLabel"
        ""
        "Part of KerbalismSystemHeatSupport. Full monorepo history is in the GitHub repository."
        ""
        "---"
        ""
    )

    $capture = $false
    $modLines = New-Object System.Collections.Generic.List[string]

    foreach ($line in $lines) {
        if ($line -match "^## $([regex]::Escape($ModId))") {
            $capture = $true
            $modLines.Add($line) | Out-Null
            continue
        }
        if ($capture -and $line -eq "---") {
            break
        }
        if ($capture) {
            $modLines.Add($line) | Out-Null
        }
    }

    if ($modLines.Count -eq 0) {
        throw "Could not find changelog section for $ModId in $SourcePath"
    }

    # Drop upstream doc links that only work in the repo
    $body = ($modLines | Where-Object { $_ -notmatch '^\s*Upstream history:' }) -join "`r`n"
    return ($header -join "`r`n") + $body + "`r`n"
}

function Get-ModReadme {
    param([string] $SourcePath)

    $text = Get-Content -LiteralPath $SourcePath -Raw
    $text = $text -replace '(?m)^> Part of \[KerbalismSystemHeatSupport\].*\r?\n', ''
    $text = $text -replace '\]\(\.\./\.\./LICENSE\)', '](LICENSE)'
    return $text.TrimStart() + "`r`n"
}

if (-not $SkipBuild) {
    $msbuild = Find-MSBuild
    Write-Host "Building Release..."
    & $msbuild "src\KerbalismSystemHeatSupport.sln" /p:Configuration=Release /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE" }
}

$outPath = Join-Path $RepoRoot $OutputDir
if (Test-Path $outPath) {
    Remove-Item -LiteralPath $outPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outPath | Out-Null

$stagingRoot = Join-Path $env:TEMP "KerbalismSystemHeatSupport-pack-$VersionTag"
if (Test-Path $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

foreach ($mod in $Mods) {
    $gd = Join-Path $RepoRoot $mod.GameDataDir
    $dll = Join-Path $gd "PluginData\$($mod.DllName)"
    if (-not (Test-Path $dll)) {
        throw "Missing built DLL: $dll (run Release build first)"
    }

    $stage = Join-Path $stagingRoot $mod.Id
    New-Item -ItemType Directory -Path (Join-Path $stage "GameData") -Force | Out-Null

    Copy-Item -LiteralPath $gd -Destination (Join-Path $stage "GameData\$($mod.Id)") -Recurse

    if ($mod.ExtrasDir) {
        $extras = Join-Path $RepoRoot $mod.ExtrasDir
        if (Test-Path $extras) {
            New-Item -ItemType Directory -Path (Join-Path $stage "Extras") -Force | Out-Null
            Copy-Item -LiteralPath $extras -Destination (Join-Path $stage "Extras\$($mod.Id)") -Recurse
        }
    }

    Copy-Item -LiteralPath (Join-Path $RepoRoot "LICENSE") -Destination (Join-Path $stage "LICENSE")

    $readme = Get-ModReadme -SourcePath (Join-Path $RepoRoot $mod.ReadmeSource)
    Set-Content -LiteralPath (Join-Path $stage "README.md") -Value $readme -NoNewline

    $changelog = Get-ModChangelog -ModId $mod.Id -SourcePath (Join-Path $RepoRoot "CHANGELOG.md")
    Set-Content -LiteralPath (Join-Path $stage "CHANGELOG.md") -Value $changelog -NoNewline

    $zipName = "$($mod.ReleaseName).$VersionLabel.zip"
    $zipPath = Join-Path $outPath $zipName
    if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

    $items = Get-ChildItem -LiteralPath $stage | Select-Object -ExpandProperty FullName
    Compress-Archive -Path $items -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Created $zipName ($((Get-Item -LiteralPath $zipPath).Length) bytes)"
}

Remove-Item -LiteralPath $stagingRoot -Recurse -Force
Write-Host ""
Write-Host "Done. Packages in: $outPath"
