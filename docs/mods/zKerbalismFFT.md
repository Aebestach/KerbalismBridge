> Part of [KerbalismSystemHeatSupport](../../README.md). Build: `src/KerbalismSystemHeatSupport.sln`.
# Kerbalism FarFutureTechnologies

**Version:** 1.0.0

Community fork of [judicator/KerbalismFFT](https://github.com/judicator/KerbalismFFT), originally by [Alexander Rogov](https://github.com/judicator). Maintained at [Aebestach/KerbalismFFT](https://github.com/Aebestach/KerbalismFFT). This is **not** an official judicator or original-author release.

"Middleman" mod that implements experimental Kerbalism resource system support for [Nertea's Far Future Technologies](https://forum.kerbalspaceprogram.com/index.php?/topic/199070-*/).


## What parts and features of Far Future Technologies mod are supported and how?

### Antimatter tanks

* Planner in VAB/Hangar: information about EC consumption of antimatter containment.
* EC consumption for active vessel: works like FFT, but uses the Kerbalism EC consumption/production system.
* EC consumption for unloaded vessels. If a vessel runs out of electric charge, antimatter containment shuts down and all antimatter annihilates. Resulting thermal energy is applied to the antimatter tank on vessel load, which can destroy the tank. Do not leave antimatter tanks without power.

### Fusion reactors

* Planner in VAB/Hangar: EC production and De (or De/He3) consumption at the configured operating mode. While startup capacitors are charging, the planner shows charging EC draw instead of running production/consumption.
* EC production and propellant consumption on **loaded** vessels: routed through Kerbalism (native FFT `GeneratePower` resource IO is blocked when the KerbalismFFT updater is present). Reactor throttle adjusts automatically (respecting minimum throttle) to meet electricity demand. Upstream 0.2.0 only integrated **unloaded** vessels through Kerbalism.
* Capacitor charging EC before startup: routed through Kerbalism on loaded and unloaded vessels (native `RechargeCapacitors` resource IO is blocked when the updater is present).
* **Unloaded** vessels: background simulation runs at full reactor power (minimum throttle is always on; the remainder feeds the vessel EC pool). Capacitor charging is simulated in background before startup.
* Fusion waste heat on unloaded vessels is included in [KerbalismSystemHeat](https://github.com/Aebestach/KerbalismSystemHeat) background thermal simulation when that mod is installed.

### Fusion engines (`ModuleFusionEngine` with built-in fusion reactor)

Same Kerbalism routing and planner behaviour as standalone fusion reactors (`FFTFusionEngineKerbalismUpdater`). Trimodal engines that generate EC are covered.

### Particle detector science experiment

Converted to a Kerbalism science experiment (requires Kerbalism **FeatureScience**). Experiment duration is set to 2 Kerbin years.

### Engines reliability

FFT `fft-*` engines get unlimited ignitions and burn-duration limits where patched (requires Kerbalism **FeatureReliability**). Fusion reactors receive a Reliability module with very high MTBF.

### Cryogenic tanks (optional)

If the [CryoTanks](https://github.com/post-kerbin-mining-corporation/CryoTanks) mod is installed, FFT (and other) parts with `ModuleCryoTank` cooling get a `PlannerController` entry for cryogenic cooling EC in the Kerbalism planner.

### Kerbalism Automation (Planner â†?Automation â†?vessel devices)

When Kerbalism **Automation** is enabled, integrated fusion reactors and fusion-engine reactors appear as scriptable devices on loaded and unloaded vessels (for example **FFT fusion reactor**). Simplified Chinese device strings are in `Localization/zh-cn.cfg`; broker and antimatter messages are localized in `Localization/ru.cfg` (Automation device names fall back to English in Russian).


## Kerbalism profile

Adds supplies for Antimatter, LqdDeuterium, and LqdHe3 on the **KerbalismSupport** profile (`FFTKerbalismSupport.cfg`).


## Dependencies

Install these separately; they are **not** included in release packages (unlike upstream 0.1.1â€?.2.0 downloads that bundled KerbalismSystemHeat).

* [Kerbalism](https://github.com/Kerbalism/Kerbalism) â€?3.32+ recommended (Bootstrap `*.kbin` workflow)
* [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) â€?loads this mod after Kerbalism is present (same pattern as zKerbalismSystemHeat / zKerbalismNFE)
* [HarmonyKSP](https://github.com/KSPModdingLibs/HarmonyKSP) â€?required by Kerbalism 3.32+; Harmony patches need `0Harmony` at runtime (loaded with Kerbalism)
* [FarFutureTechnologies](https://github.com/post-kerbin-mining-corporation/FarFutureTechnologies)
* [SystemHeat](https://github.com/post-kerbin-mining-corporation/SystemHeat) â€?required by FFT and this mod's assembly load order
* [KerbalismSystemHeat (1.0.0+)](https://github.com/Aebestach/KerbalismSystemHeat) â€?**strongly recommended**; required for fusion waste heat in unloaded-vessel SystemHeat loop simulation (optional at compile time; detected at runtime via reflection)
* [Module Manager (latest preferred)](https://github.com/sarbian/ModuleManager)
* [CryoTanks](https://github.com/post-kerbin-mining-corporation/CryoTanks) â€?optional; enables cryo-tank planner patches only


## Installation

Remove any existing `zKerbalismFFT` folder from `GameData` before installing. If upgrading from a preâ€“PluginHost build, delete `GameData/zKerbalismFFT/Plugin/` (the old autoload path).

Then merge the `GameData` folder from the release archive into your Kerbal Space Program `GameData` folder.

The plugin is loaded by **zKerbalismPluginHost** from:

- `GameData/zKerbalismFFT/zKerbalismFFT.host.xml`
- `GameData/zKerbalismFFT/PluginData/zKerbalismFFT.dll`

### Building from Visual Studio

Open `src/KerbalismSystemHeatSupport.sln` and build the **zKerbalismFFT** project. Output is written directly to:

- `GameData/zKerbalismFFT/PluginData/zKerbalismFFT.dll`

Intermediate files stay under `src/obj` (not `bin/Release`). KSP/Kerbalism/FFT references use the shared `KSPDLL` layout next to the repo.


## Mod settings

In `GameData/zKerbalismFFT/Settings.cfg`:

* `FFT_Engines_Radioactivity_Coeff` â€?multiplies radiation from static FFT engine emitters (default `1.0`; lower if engines feel too radioactive)
* `FFT_FusionReactors_Radioactivity_Coeff` â€?multiplies radiation from fusion reactor emitters (default `1.0`)


## Optional patch

Optional patch in `Extras/FFTFusionReactorsLowerMinThrust`: lowers fusion reactor minimum throttle from 10% to 5%.

Copy the `FFTFusionReactorsLowerMinThrust` folder into `GameData` to enable it.


## Licensing

Licensed under the [MIT License](../../LICENSE).

Copyright (c) 2022 Alexander Rogov  
Copyright (c) 2026 Aebestach

Runtime dependencies (Kerbalism, FarFutureTechnologies, SystemHeat, KerbalismSystemHeat, ModuleManager, HarmonyKSP, etc.) remain under their respective licenses and must be obtained separately.
