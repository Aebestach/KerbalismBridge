# Kerbalism Bridge

**Kerbalism Bridge** integrates Kerbalism with **SystemHeat**, **Near Future Electrical**, **Far Future Technologies**, and optional **dynamic radiation**. One repository builds several installable `GameData` packages: a **main bridge** (three DLLs) plus **satellite** mods you can add as needed.

**Version:** 1.0.0 (mod family release)

All Bridge plugins load through [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) after Kerbalism is present. **Do not** put Bridge DLLs in `Plugins/`.

---

## How integration works: Layer A and Layer B

Kerbalism Bridge does **not** use one integration style for every part. It splits work into two layers (legacy names **Layer A** and **Layer B**). Pick the layer that matches what the part **originally** uses. **Do not mix both layers on the same part.**

| | **Layer A — Process layer** | **Layer B — Native layer** |
|---|------------------------------|----------------------------|
| **GameData / DLL** | `zKerbalismProcess` | `zKerbalismNative` |
| **Best for** | Stock-style or Kerbalism-replaceable converters, harvesters, **fuel cells** | Mod **custom native** C# modules |
| **Approach** | MM swaps the part to Kerbalism `ProcessController` / `Harvester`; optionally upgrades to `ProcessControllerSystemHeat` / `HarvesterSystemHeat` | **Keeps** the mod’s native module; adds a `*KerbalismUpdater` sidecar |
| **Resources** | Kerbalism processes, brokers, background sim | Harmony blocks native resource IO; Kerbalism accounts for consumption / production |
| **Heat** | Optional SystemHeat loop via `ModuleSystemHeat` when you want loop waste heat | Native module still drives heat (e.g. `UpdateFlux()`); works with or without SystemHeat depending on the mod |
| **Recipes** | Usually needs Kerbalism Profile + Configure | Usually **no** extra ISRU Profile |

**Quick rule:**

```
Part has ModuleResourceConverter / ModuleResourceHarvester
(or a mod pack already replaced it with ProcessController)
  → Layer A (Process)

Part has a mod-native module (ModuleSystemHeat*, FusionReactor, DischargeCapacitor, …)
  → Layer B (Native) — add Updater; do not replace with ProcessControllerSystemHeat
```

**Examples**

- **Layer A:** Kerbalism chemical plants / drills; Sterling MAEC **fuel cells**; FFT industrial smelters (Process + optional SystemHeat).
- **Layer B core:** SystemHeat fission reactors / engines; generic SH converters/harvesters.
- **Layer B satellites:** NFE (`zKerbalismNFE`), SpaceDust, Cryo, FFT fusion/antimatter (`zKerbalismFFT`), etc.

Full architecture write-up: [docs/architecture/KerbalismBridge-Architecture-en.md](docs/architecture/KerbalismBridge-Architecture-en.md) (中文: [KerbalismBridge-Architecture.md](docs/architecture/KerbalismBridge-Architecture.md)).

---

## Packages

### Main bridge (minimum SystemHeat integration)

Install all three for a typical SystemHeat + Kerbalism setup.

| GameData folder | DLL | Role |
|-----------------|-----|------|
| `zKerbalismBridge` | `zKerbalismBridge.dll` | **Shared runtime** — Harmony bootstrap, background thermal sim, editor sim, settings. **Not** Layer A or B; required by Process and Native. |
| `zKerbalismProcess` | `zKerbalismProcess.dll` | **Layer A (Process)** — `ProcessControllerSystemHeat`, `HarvesterSystemHeat`, converter / harvester / radiator MM |
| `zKerbalismNative` | `zKerbalismNative.dll` | **Layer B core (Native)** — generic SystemHeat `*KerbalismUpdater`, fission reactors/engines |

Load order: **Bridge → Process / Native** (each `*.host.xml` declares `RequireAssembly` for `zKerbalismBridge`).

```
Kerbalism
    └── zKerbalismBridge          ← runtime
            ├── zKerbalismProcess ← Layer A (+ SystemHeat when patches apply)
            └── zKerbalismNative  ← Layer B (per-mod :NEEDS[...])
```

### Satellites (optional)

| GameData folder | DLL | Role |
|-----------------|-----|------|
| `zKerbalismFFT` | `zKerbalismFFT.dll` | Far Future Technologies — profile, industrial **Layer A** cfg, fusion / antimatter **Layer B** C# |
| `zKerbalismDynamicRadiation` | `zKerbalismDynamicRadiation.dll` | Post-shutdown radiation decay on integrated fission / fusion parts |
| `zKerbalismCryo` | `zKerbalismCryo.dll` | CryoTanks + SystemHeat cryogenic tanks — Kerbalism EC/background boiloff |
| `zKerbalismNFE` | `zKerbalismNFE.dll` | Near Future Electrical — capacitors, nuclear recycler (Layer B) |
| `zKerbalismSpaceDust` | `zKerbalismSpaceDust.dll` | SpaceDust harvesters (Layer B) |

**SterlingSystemsKerbalism** is maintained by Sterling Systems; this repo only ships `SterlingSystems.cfg` as the FINAL heat bridge under Process.

---

## Requirements

| Required | Notes |
|----------|-------|
| [Kerbalism](https://github.com/Kerbalism/Kerbalism) 3.32+ | Bootstrap `*.kbin` workflow |
| [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) | Deferred loader for Bridge DLLs |
| [Module Manager](https://github.com/sarbian/ModuleManager) | Patches |

Per-package dependencies (SystemHeat, FFT, NFE, …): see [docs/mods/](docs/mods/) — **player-facing READMEs copied into release zips**.

---

## Installation

1. Install Kerbalism, Module Manager, and **zKerbalismPluginHost**.
2. Remove legacy `GameData/zKerbalismSystemHeat` and any old `Plugins/` copies of Bridge DLLs. Replace pre-1.0 monolithic `zKerbalismNFE` (capacitors) with the new **`zKerbalismNFE` satellite** if you use NFE.
3. Copy **`zKerbalismBridge` + `zKerbalismProcess` + `zKerbalismNative`** into `GameData` (minimum bridge).
4. Add `zKerbalismFFT` / `zKerbalismDynamicRadiation` if you use those mods.
5. Delete `ModuleManager.ConfigCache` and restart KSP.

---

## Building

Open `src/KerbalismBridge.sln` in Visual Studio and build **Release**. KSP references: `../KSPDLL/` (sibling folder under `C#/`).

```text
msbuild src\KerbalismBridge.sln /p:Configuration=Release
```

Outputs:

```text
GameData/zKerbalismBridge/PluginData/zKerbalismBridge.dll
GameData/zKerbalismProcess/PluginData/zKerbalismProcess.dll
GameData/zKerbalismNative/PluginData/zKerbalismNative.dll
GameData/zKerbalismFFT/PluginData/zKerbalismFFT.dll
GameData/zKerbalismDynamicRadiation/PluginData/zKerbalismDynamicRadiation.dll
```

Build **Bridge** before Process / Native on a clean tree (solution project dependencies).

---

## Release packages

```powershell
.\scripts\package-release.ps1 -Version 1.0.0
```

Produces eight zips: **KerbalismBridge**, **KerbalismProcess**, **KerbalismNative**, **KerbalismFFT**, **KerbalismDynamicRadiation**, **KerbalismCryo**, **KerbalismNFE**, **KerbalismSpaceDust**. Each zip includes that mod’s README from `docs/mods/`.

---

## Documentation

| Path | Purpose |
|------|---------|
| [README-CN.md](README-CN.md) | 中文版仓库说明 |
| [docs/architecture/](docs/architecture/) | Process / Native (Layer A / B) architecture |
| [docs/mods/](docs/mods/) | Per-mod install & feature docs (also shipped in releases) |
| [CHANGELOG.md](CHANGELOG.md) | Version history |
| [docs/legal/ATTRIBUTION.md](docs/legal/ATTRIBUTION.md) | Fork and copyright notices |

---

## Licensing

See [LICENSE](LICENSE). Runtime dependencies remain under their respective licenses.
