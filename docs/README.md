# Documentation

| Section | Purpose |
|---------|---------|
| [architecture/](architecture/) | **Kerbalism Bridge architecture** — Process / Native layers (Layer A / B), package layout, design rules |
| [mods/](mods/) | **Per-mod player docs** — install steps, dependencies, and feature lists for each `GameData` package; copied into release zips as `README.md` |
| [legal/ATTRIBUTION.md](legal/ATTRIBUTION.md) | Fork and copyright notices |

Repository overview: [../README.md](../README.md) · 中文: [../README-CN.md](../README-CN.md)  
Changelog: [../CHANGELOG.md](../CHANGELOG.md)

## Architecture

| Doc | Language |
|-----|----------|
| [KerbalismBridge-Architecture-en.md](architecture/KerbalismBridge-Architecture-en.md) | English |
| [KerbalismBridge-Architecture.md](architecture/KerbalismBridge-Architecture.md) | 中文 |

## Mod packages (`docs/mods/`)

Each file describes **one installable mod** (or the main-bridge trio for SystemHeat). These are **not** developer architecture notes — they answer “what does this folder do, what do I need installed, what parts are supported?”

Build all plugins from [`src/KerbalismBridge.sln`](../src/KerbalismBridge.sln).

| Doc | GameData folder | DLL |
|-----|-----------------|-----|
| [KerbalismBridge.md](mods/KerbalismBridge.md) | `zKerbalismBridge` + `zKerbalismProcess` + `zKerbalismNative` | three DLLs |
| [zKerbalismFFT.md](mods/zKerbalismFFT.md) | `zKerbalismFFT` | `zKerbalismFFT.dll` |
| [zKerbalismDynamicRadiation.md](mods/zKerbalismDynamicRadiation.md) | `zKerbalismDynamicRadiation` | `zKerbalismDynamicRadiation.dll` |
