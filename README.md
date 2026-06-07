# Kerbalism Bridge

<div align="center">

<img src="https://imgur.com/ON8D2wc.jpg" alt="Banner"/>

</div>

**English** · [中文](README-CN.md)

Kerbalism Bridge extends **Kerbalism** to advanced power and thermal parts — reactors, converters, harvesters, capacitors, cryo tanks, and more. Those parts show up in the **Kerbalism planner**, pull from Kerbalism resource stores **in flight**, and **keep simulating on unloaded vessels** during timewarp instead of freezing at unload-time values.

The download ships as several optional `GameData` packages: a **main bridge** (minimum for SystemHeat) plus **satellite** mods you add only when you need them.

---

## Lineage and improvements

This project continues the work of [judicator/KerbalismSystemHeat](https://github.com/judicator/KerbalismSystemHeat) and [judicator/KerbalismFFT](https://github.com/judicator/KerbalismFFT) (originally by Alexander Rogov). It is maintained at [Aebestach/KerbalismBridge](https://github.com/Aebestach/KerbalismBridge) and is **not** an official judicator release.

**Compared with upstream KerbalismSystemHeat**, Kerbalism Bridge keeps the core idea — Kerbalism resource accounting for SystemHeat parts, planner support, and background simulation — and adds:

- **Loaded-vessel integration** for fission reactors and engines (upstream mainly covered unloaded vessels).
- **Background thermal simulation** for SystemHeat loops on unloaded vessels, so long timewarps do not leave heat loops frozen at unload-time values.
- **Two integration layers (Layer A / Layer B)** instead of one style for every part — see below.
- **Optional satellites** for NFE capacitors, SpaceDust harvesters, CryoTanks, dynamic radiation decay, and more.
- **zKerbalismPluginHost** loading from `PluginData/` (do not put Bridge DLLs in `Plugins/`).

**Compared with upstream KerbalismFFT**, this fork keeps antimatter containment, fusion reactor/engine planner and background behaviour, science and reliability patches, and industrial FFT processors — and improves:

- **Loaded-vessel Kerbalism routing** for fusion reactors (power and propellant), not only background simulation.
- **Fusion waste heat** in Bridge background thermal sim when the main bridge is installed.
- **CryoTanks** moved to a separate **zKerbalismCryo** satellite for clearer maintenance.

Version history: [CHANGELOG.md](CHANGELOG.md).

---

## Layer A and Layer B (brief)

Bridge does **not** use one integration style for every part. Pick **one** layer per part:

| | **Layer A — Process** | **Layer B — Native** |
|---|------------------------|----------------------|
| **Package** | `zKerbalismProcess` | `zKerbalismNative` + optional satellites |
| **Best for** | Stock-style converters, harvesters, fuel cells | Mod-native C# modules (fission, fusion, capacitors, SpaceDust, cryo tanks, …) |
| **Approach** | Part runs Kerbalism processes; optional SystemHeat loop heat | Native module stays; a sidecar routes resources through Kerbalism |

**Quick rule:** parts that already use Kerbalism `ProcessController` / `Harvester` → **Layer A**. Parts with mod-specific modules (SystemHeat fission, FFT fusion, NFE capacitors, …) → **Layer B**. Do not mix both on the same part.

---

## Packages

### Main bridge (minimum for SystemHeat)

| GameData folder | Role |
|-----------------|------|
| `zKerbalismBridge` | Shared runtime — background thermal sim, editor sim, settings |
| `zKerbalismProcess` | Layer A — converters, harvesters, radiators |
| `zKerbalismNative` | Layer B core — SystemHeat fission, generic SH converters/harvesters |

### Satellites (optional)

| GameData folder | Role |
|-----------------|------|
| `zKerbalismFFT` | Far Future Technologies — antimatter, fusion, science, industrial plants |
| `zKerbalismDynamicRadiation` | Post-shutdown radiation decay on integrated fission / fusion parts |
| `zKerbalismCryo` | CryoTanks + SystemHeat cryogenic tanks |
| `zKerbalismNFE` | Near Future Electrical — discharge capacitors |
| `zKerbalismSpaceDust` | SpaceDust harvesters |
| `zKerbalismSterlingSystems` | Sterling Systems — fission, converters, engines, and more (now maintained here; thanks to [JadeOfMaar](https://github.com/JadeOfMaar) and [NerdyBoy](https://github.com/NerdyBoy709) for their earlier work on SterlingSystemsKerbalism) |

---

## Requirements

| Required | Notes |
|----------|-------|
| [Kerbalism](https://github.com/Kerbalism/Kerbalism) 3.32+ | Bootstrap `*.kbin` workflow |
| [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) | Deferred loader for Bridge DLLs |
| [Module Manager](https://github.com/sarbian/ModuleManager) | Patches |

---

## Installation

1. Install Kerbalism, Module Manager, and **zKerbalismPluginHost**.
2. Remove legacy `GameData/zKerbalismSystemHeat` and any old `Plugins/` copies of Bridge DLLs. If upgrading from pre-1.0 Bridge, install the new **`zKerbalismNFE`** satellite for NFE capacitors.
3. Copy **`zKerbalismBridge` + `zKerbalismProcess` + `zKerbalismNative`** into `GameData` (minimum bridge).
4. Add satellites as needed (`zKerbalismFFT`, `zKerbalismNFE`, `zKerbalismSpaceDust`, `zKerbalismCryo`, `zKerbalismDynamicRadiation`, `zKerbalismSterlingSystems`).
5. Delete `ModuleManager.ConfigCache` and restart KSP.

---

## Settings

Tunable defaults live in each package's `Settings.cfg`:

| File | Notable keys |
|------|--------------|
| `zKerbalismBridge/Settings.cfg` | `BackgroundThermalSim`, `BackgroundRadiatorCoefficient` |
| `zKerbalismDynamicRadiation/Settings.cfg` | `Reactor_*` / `Engine_*` min emission and decay rates |
| `zKerbalismCryo/Settings.cfg` | `Enabled` |
| `zKerbalismFFT/Settings.cfg` | Antimatter background detonation; FFT radioactivity coefficients |

---

## Documentation

| Path | Purpose |
|------|---------|
| [CHANGELOG.md](CHANGELOG.md) | Version history |
| [docs/DEVELOPER.md](docs/DEVELOPER.md) | Build, release, and architecture (developers) |
| [docs/legal/ATTRIBUTION.md](docs/legal/ATTRIBUTION.md) | Fork and copyright notices |

---

## Licensing

See [LICENSE](LICENSE). Runtime dependencies remain under their respective licenses.
