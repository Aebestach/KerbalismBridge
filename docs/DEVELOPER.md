# Developer guide

Repository overview: [../README.md](../README.md) · 中文: [DEVELOPER-CN.md](DEVELOPER-CN.md)

---

## Solution and packages

Open `src/KerbalismBridge.sln` in Visual Studio. KSP assembly references live in `../KSPDLL/` (sibling folder under `C#/`).

| Project | Output DLL | GameData folder |
|---------|------------|-----------------|
| zKerbalismBridge | `zKerbalismBridge.dll` | `GameData/zKerbalismBridge/PluginData/` |
| zKerbalismProcess | `zKerbalismProcess.dll` | `GameData/zKerbalismProcess/PluginData/` |
| zKerbalismNative | `zKerbalismNative.dll` | `GameData/zKerbalismNative/PluginData/` |
| zKerbalismFFT | `zKerbalismFFT.dll` | `GameData/zKerbalismFFT/PluginData/` |
| zKerbalismDynamicRadiation | `zKerbalismDynamicRadiation.dll` | `GameData/zKerbalismDynamicRadiation/PluginData/` |
| zKerbalismCryo | `zKerbalismCryo.dll` | `GameData/zKerbalismCryo/PluginData/` |
| zKerbalismNFE | `zKerbalismNFE.dll` | `GameData/zKerbalismNFE/PluginData/` |
| zKerbalismSpaceDust | `zKerbalismSpaceDust.dll` | `GameData/zKerbalismSpaceDust/PluginData/` |

Build **Release**. On a clean tree, build **Bridge** before Process / Native (solution declares project dependencies). Intermediate files stay under each project's `obj/`; DLLs are written directly to `GameData/.../PluginData/`.

```text
msbuild src\KerbalismBridge.sln /p:Configuration=Release
```

All plugins load through [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) (`*.host.xml` + `PluginData/`). Do not place Bridge DLLs in `Plugins/`.

---

## Architecture

Integration uses **Layer A (Process)** and **Layer B (Native)**. Full design rules, module names, and patch layout:

| Doc | Language |
|-----|----------|
| [architecture/KerbalismBridge-Architecture-en.md](architecture/KerbalismBridge-Architecture-en.md) | English |
| [architecture/KerbalismBridge-Architecture.md](architecture/KerbalismBridge-Architecture.md) | 中文 |

**SterlingSystemsKerbalism** is external; this repo only ships `GameData/zKerbalismProcess/Patches/ModsSupport/SterlingSystems.cfg` as the FINAL heat bridge.

---

## Release packaging

Scripts live under `scripts/`. Run from the **repository root** (the script resolves the repo from its own path, but the examples below assume the current directory is the root).

| File | Purpose |
|------|---------|
| `scripts/package-release.ps1` | PowerShell entry point |
| `scripts/package-release.cmd` | CMD entry point (calls the `.ps1` script) |

**Note:** CMD **cannot** run `.ps1` files directly. Windows usually opens them in an editor instead of executing them. Use `package-release.cmd` from CMD.

### PowerShell

```powershell
cd path\to\KerbalismBridge
.\scripts\package-release.ps1 -Version 1.0.0
.\scripts\package-release.ps1 -Version 1.0.0-beta.1 -SkipBuild
```

If execution is blocked, allow scripts for the current window only:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

### CMD

```text
cd /d path\to\KerbalismBridge
scripts\package-release.cmd -Version 1.0.0
scripts\package-release.cmd -Version 1.0.0 -SkipBuild
```

### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-Version` | yes | Release label, e.g. `1.0.0`, `1.0.0-beta.1`, or `v1.0.0` |
| `-SkipBuild` | no | Skip MSBuild; requires DLLs already under `GameData/.../PluginData/` |
| `-OutputDir` | no | Output folder (default `dist`) |

By default the script builds `src\KerbalismBridge.sln` in **Release** (Visual Studio or Build Tools with MSBuild required). With `-SkipBuild`, it only packs existing DLLs.

### Output

Produces eight zips under `dist/`: **KerbalismBridge**, **KerbalismProcess**, **KerbalismNative**, **KerbalismFFT**, **KerbalismDynamicRadiation**, **KerbalismCryo**, **KerbalismNFE**, **KerbalismSpaceDust** (names like `KerbalismBridge.v1.0.0.zip`).

Each zip contains `GameData/`, `LICENSE`, a short `README.md`, and a per-mod `CHANGELOG.md` excerpt from the root [CHANGELOG.md](../CHANGELOG.md).

On success the console prints `Created KerbalismBridge.v...zip` lines and `Done. Packages in: ...\dist`.

---

## Other docs

| Path | Purpose |
|------|---------|
| [../CHANGELOG.md](../CHANGELOG.md) | Version history |
| [legal/ATTRIBUTION.md](legal/ATTRIBUTION.md) | Fork and copyright notices |
| [../LICENSE](../LICENSE) | MIT license text |
