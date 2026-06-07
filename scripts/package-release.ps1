#Requires -Version 5.1
<#
.SYNOPSIS
  Build Release (optional) and pack eight KSP mod zips for GitHub Release.

.DESCRIPTION
  Each zip contains:
    GameData/zKerbalismXXX/
    LICENSE
    README.md           (short pointer to repo + CHANGELOG)
    CHANGELOG.md        (monorepo header + this mod's section from root CHANGELOG)

  Output: dist/KerbalismBridge.v<Version>.zip (Version is manual, e.g. 1.0.0-beta.1)

  Run from PowerShell, or from CMD use package-release.cmd (CMD opens .ps1 in an editor).

.EXAMPLE
  .\scripts\package-release.ps1 -Version 1.0.0

.EXAMPLE
  .\scripts\package-release.cmd -Version 1.0.0 -SkipBuild

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
        Id           = "zKerbalismBridge"
        ReleaseName  = "KerbalismBridge"
        GameDataDir  = "GameData\zKerbalismBridge"
        DllName      = "zKerbalismBridge.dll"
        ChangelogId  = "zKerbalismBridge"
    },
    @{
        Id           = "zKerbalismProcess"
        ReleaseName  = "KerbalismProcess"
        GameDataDir  = "GameData\zKerbalismProcess"
        DllName      = "zKerbalismProcess.dll"
        ChangelogId  = "zKerbalismProcess"
    },
    @{
        Id           = "zKerbalismNative"
        ReleaseName  = "KerbalismNative"
        GameDataDir  = "GameData\zKerbalismNative"
        DllName      = "zKerbalismNative.dll"
        ChangelogId  = "zKerbalismNative"
    },
    @{
        Id           = "zKerbalismFFT"
        ReleaseName  = "KerbalismFFT"
        GameDataDir  = "GameData\zKerbalismFFT"
        DllName      = "zKerbalismFFT.dll"
        ChangelogId  = "zKerbalismFFT"
    },
    @{
        Id           = "zKerbalismDynamicRadiation"
        ReleaseName  = "KerbalismDynamicRadiation"
        GameDataDir  = "GameData\zKerbalismDynamicRadiation"
        DllName      = "zKerbalismDynamicRadiation.dll"
        ChangelogId  = "zKerbalismDynamicRadiation"
    },
    @{
        Id           = "zKerbalismCryo"
        ReleaseName  = "KerbalismCryo"
        GameDataDir  = "GameData\zKerbalismCryo"
        DllName      = "zKerbalismCryo.dll"
        ChangelogId  = "zKerbalismCryo"
    },
    @{
        Id           = "zKerbalismNFE"
        ReleaseName  = "KerbalismNFE"
        GameDataDir  = "GameData\zKerbalismNFE"
        DllName      = "zKerbalismNFE.dll"
        ChangelogId  = "zKerbalismNFE"
    },
    @{
        Id           = "zKerbalismSpaceDust"
        ReleaseName  = "KerbalismSpaceDust"
        GameDataDir  = "GameData\zKerbalismSpaceDust"
        DllName      = "zKerbalismSpaceDust.dll"
        ChangelogId  = "zKerbalismSpaceDust"
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
        "Part of KerbalismBridge. Full monorepo history is in the GitHub repository."
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

    $body = ($modLines -join "`r`n")
    return ($header -join "`r`n") + $body + "`r`n"
}

function Get-ModReadme {
    param(
        [string] $ModId,
        [string] $ReleaseName
    )

    return @"
# $ReleaseName

Part of [Kerbalism Bridge](https://github.com/Aebestach/KerbalismBridge) (`$ModId`).

**Version:** $VersionLabel

Features, dependencies, settings, and install notes: see **CHANGELOG.md** in this archive.

Full repository: https://github.com/Aebestach/KerbalismBridge

"@
}

if (-not $SkipBuild) {
    $msbuild = Find-MSBuild
    Write-Host "Building Release..."
    & $msbuild "src\KerbalismBridge.sln" /p:Configuration=Release /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE" }
}

$outPath = Join-Path $RepoRoot $OutputDir
if (Test-Path $outPath) {
    Remove-Item -LiteralPath $outPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outPath | Out-Null

$stagingRoot = Join-Path $env:TEMP "KerbalismBridge-pack-$VersionTag"
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

    Copy-Item -LiteralPath (Join-Path $RepoRoot "LICENSE") -Destination (Join-Path $stage "LICENSE")

    $readme = Get-ModReadme -ModId $mod.Id -ReleaseName $mod.ReleaseName
    Set-Content -LiteralPath (Join-Path $stage "README.md") -Value $readme -NoNewline

    $changelogModId = if ($mod.ChangelogId) { $mod.ChangelogId } else { $mod.Id }
    $changelog = Get-ModChangelog -ModId $changelogModId -SourcePath (Join-Path $RepoRoot "CHANGELOG.md")
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
