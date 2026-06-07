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

```powershell
.\scripts\package-release.ps1 -Version 1.0.0
```

Produces eight zips under `dist/`: **KerbalismBridge**, **KerbalismProcess**, **KerbalismNative**, **KerbalismFFT**, **KerbalismDynamicRadiation**, **KerbalismCryo**, **KerbalismNFE**, **KerbalismSpaceDust**. Each zip contains `GameData/`, `LICENSE`, `README.md` (pointer), and a per-mod `CHANGELOG.md` excerpt from the root [CHANGELOG.md](../CHANGELOG.md).

Use `-SkipBuild` when DLLs are already built.

---

## Other docs

| Path | Purpose |
|------|---------|
| [../CHANGELOG.md](../CHANGELOG.md) | Per-package features, dependencies, settings, version history |
| [legal/ATTRIBUTION.md](legal/ATTRIBUTION.md) | Fork and copyright notices |
| [../LICENSE](../LICENSE) | MIT license text |
