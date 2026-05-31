> Part of [KerbalismSystemHeatSupport](../../README.md). Build: `src/KerbalismSystemHeatSupport.sln`.
# Kerbalism SystemHeat

**Version:** 1.0.0

Community fork of [judicator/KerbalismSystemHeat](https://github.com/judicator/KerbalismSystemHeat), originally by [Alexander Rogov](https://github.com/judicator). Maintained at [Aebestach/KerbalismSystemHeat](https://github.com/Aebestach/KerbalismSystemHeat). This is **not** an official judicator or original-author release.

"Middleman" mod that implements Kerbalism resource system support for [Nertea's SystemHeat](https://forum.kerbalspaceprogram.com/index.php?/topic/193909-*/).


## What partmodules and features of SystemHeat mod are supported and how?

### Background resource production/consumption for unloaded vessels

Implemented for: radiators, converters, harvesters, fission reactors, and fission engines.

Converters and harvesters use Kerbalism `ProcessController` / `Harvester` background logic (plus thermal updates from this mod). Radiators consume EC when cooling. Fission reactors and trimodal fission engines that produce EC run a full-throttle background model (minimum throttle always on; the remainder feeds the vessel EC pool). Manual throttle limits from the last loaded state are respected when manual control is enabled.

### Background thermal simulation for unloaded vessels

When `BackgroundThermalSim` is enabled in `Settings.cfg`, KerbalismSystemHeat runs a simplified SystemHeat loop thermal balance during Kerbalism background simulation. Persisted loop temperature and flux are updated for unloaded vessels instead of staying frozen at unload-time values.

Supported heat sources: converters, harvesters, fission reactors, and radiators. With [KerbalismFFT](https://github.com/Aebestach/KerbalismFFT) installed, fusion reactor and fusion engine waste heat is included as well.

This is not a full SystemHeat simulation â€?it is a minimal one-step approximation to keep long timewarps from leaving heat loops unrealistically stale. Loaded vessels still use native SystemHeat simulation.

Upstream [judicator/KerbalismSystemHeat 0.5.0](https://github.com/judicator/KerbalismSystemHeat) did not simulate heat loops in the background; loop state was only refreshed when the vessel loaded again.

### Kerbalism resource production/consumption system for active vessels

Implemented for: radiators, converters, harvesters, fission reactors, and fission engines.

Fission reactors and engines on loaded vessels route EC production and fuel consumption through Kerbalism (`ResourceUpdate` and Harmony patches that block native SystemHeat resource IO). Reactor throttle adjusts automatically (respecting min/max throttle and manual mode) to meet electricity demand via native `CalculateGoalThrottle`. This avoids Kerbalism "incoherent behavior at high warp speed" warnings for those parts.

Upstream 0.5.0 integrated fission parts through Kerbalism only on **unloaded** vessels; loaded vessels still used stock SystemHeat resource IO.

Heat generation on loaded vessels is still handled by native SystemHeat modules; only resource accounting is routed through Kerbalism.

### Kerbalism planner support in VAB/Hangar

Implemented for: radiators, converters, harvesters, fission reactors, and fission engines.

Planner simulation for converters and harvesters follows loop temperature: process rates scale with thermal efficiency from the SystemHeat loop. In VAB/SPH, starting a converter or harvester updates Kerbalism EC estimates immediately, and the part keeps running while the loop heats up (auto-shutdown is disabled in the editor; it still applies in flight).

You no longer need to enable SystemHeat "heat simulation" in the PAW for planner integration â€?efficiency follows the loop temperature used by `ProcessControllerSystemHeat` / `HarvesterSystemHeat`.

"Simulated resource abundance" on harvesters (same idea as the stock Kerbalism `Harvester` module) is still available for convenience.

### Kerbalism Automation (Planner â†?Automation â†?vessel devices)

When Kerbalism **Automation** is enabled, integrated fission reactors and trimodal fission-engine reactors appear as scriptable devices on loaded and unloaded vessels (for example **System Heat fission reactor**). Simplified Chinese device strings are in `Localization/zh-cn.cfg`; broker and warning messages are localized in `Localization/ru.cfg` (Automation device names fall back to English in Russian).


## Part modules (vs upstream 0.5.0)

| Role | Fork 1.0.0 module | Upstream 0.5.0 |
|------|-------------------|----------------|
| Radiators | `SystemHeatRadiatorKerbalism` | same |
| Converters | `ProcessControllerSystemHeat` | `SystemHeatConverterKerbalism` |
| Harvesters | `HarvesterSystemHeat` | `SystemHeatHarvesterKerbalism` |
| Fission reactors / engines | `SystemHeatFissionReactorKerbalismUpdater` / `SystemHeatFissionEngineKerbalismUpdater` | same family |


## Kerbalism profiles support

Default and ScienceOnly profiles have been tested. Other profiles should work when their `Configure` modules match this mod's patches.

### Converters and harvesters (Default / ScienceOnly)

Parts whose Kerbalism `Configure` module title is **Chemical Plant**, **Drill**, or **Pump** are converted by Module Manager (`Converters_ProcessControllerSystemHeat.cfg`, `Harvesters_HarvesterSystemHeat.cfg`, `:FINAL`) to `ProcessControllerSystemHeat` / `HarvesterSystemHeat`, given a `ModuleSystemHeat` loop when missing, and wired in `Configure` SETUP blocks. This covers stock Kerbalism ISRU, drills, and NFE processes added onto chemical plants (for example Uraninite centrifuge / breeder reactor).

Not converted by those rules: life-support `Configure` titles such as **Pod**, standalone RTGs (see `RTG_SystemHeat_Patches.cfg` per part), third-party parts with non-standard Configure titles, and a few special-case exclusions (for example Rational Resources + KPBS).

### Fission reactors (including NFE)

Kerbalism **Default** replaces NFE `FissionReactor` / `FissionGenerator` on `nfe-reactor-*` with a simplified Kerbalism `ProcessController` (on/off, fixed EC, no SystemHeat loop). That stripped behaviour is **not** what you get when the part also has `ModuleSystemHeatFissionReactor`.

With **SystemHeat/Extras/SystemHeatFissionReactors** installed (or NFE 2.0+ parts that already use the SystemHeat fission backend), `SystemHeatFissionReactors.cfg` removes Kerbalism's `ProcessController`, adds `SystemHeatFissionReactorKerbalismUpdater`, and routes EC/fuel through Kerbalism while **native SystemHeat still handles reactor waste heat and loop temperature**. `SystemHeat.cfg` adds a `ModuleSystemHeat` loop on the part when one is missing. Unloaded-vessel waste heat is included in `BackgroundThermalSim`.

There is **no** dedicated `nfe-reactor-*` conversion patch in this mod (unlike explicit USI reactor patches in `ModsSupport/`). NFE fission reactors depend on SystemHeat fission extras or NFE's own SystemHeat reactor modules. **KerbalismNFE** does not touch fission reactors (capacitors only). **KerbalismDynamicRadiation** only adds optional radiation decay on top of integrated SystemHeat fission parts.


## Dependencies

Install these separately; they are **not** included in release packages.

* [Kerbalism](https://github.com/Kerbalism/Kerbalism) â€?3.32+ recommended (Bootstrap `*.kbin` workflow)
* [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) â€?deferred loader for Kerbalism kbin releases
* [HarmonyKSP](https://github.com/KSPModdingLibs/HarmonyKSP) â€?required by Kerbalism 3.32+; Harmony patches in this mod need `0Harmony` at runtime (loaded with Kerbalism, not shipped here)
* [SystemHeat](https://github.com/post-kerbin-mining-corporation/SystemHeat)
* [Module Manager (latest preferred)](https://github.com/sarbian/ModuleManager)
* **SystemHeatFissionReactors** and **SystemHeatFissionEngines** (from `SystemHeat/Extras`) â€?required for fission reactor/engine patches
* **SystemHeatConverters** and **SystemHeatHarvesters** (from `SystemHeat/Extras`) â€?required for converter and harvester SystemHeat loop patches

## Mods support

* Atomic Age (original or SpaceTux Industries Recycled Parts) â€?SystemHeat support for wrap-around radiators and nuclear engines
* [Heat Control](https://github.com/post-kerbin-mining-corporation/HeatControl) â€?SystemHeat support for heat exchangers (radiators are already supported by SystemHeat)
* [Missing History](https://github.com/UmbraSpaceIndustries/USI_Core) â€?SystemHeat support for nuclear engines
* [Near Future Aeronautics](https://github.com/post-kerbin-mining-corporation/NearFutureAeronautics) â€?SystemHeat support for nuclear engines
* [Near Future Electrics](https://github.com/post-kerbin-mining-corporation/NearFutureElectrical) â€?with `SystemHeatConverters`, the nuclear recycler uses SystemHeat mechanics (`SystemHeatConverterKerbalism` on that part)
* [USI FTT](https://github.com/UmbraSpaceIndustries/FTT) â€?SystemHeat support for nuclear reactors
* [USI Core](https://github.com/UmbraSpaceIndustries/USI_Core) â€?SystemHeat support for nuclear reactors and nuclear materials containers


## Installation

Remove any existing `zKerbalismSystemHeat` or `KerbalismSystemHeat` folder from `GameData` before installing.

Then merge the `GameData` folder from the release archive into your Kerbal Space Program `GameData` folder, and install **zKerbalismPluginHost** separately.

The plugin ships as a single assembly plus host manifest:

- `GameData/zKerbalismSystemHeat/PluginData/zKerbalismSystemHeat.dll` â€?plugin logic (loaded by zKerbalismPluginHost after Kerbalism is present)
- `GameData/zKerbalismSystemHeat/zKerbalismSystemHeat.host.xml` â€?host manifest (do not remove)

Do not place `zKerbalismSystemHeat.dll` in `Plugins`, or KSP will load it before Kerbalism Bootstrap and fail.

### Building from Visual Studio

Open `src/KerbalismSystemHeatSupport.sln` and build the **zKerbalismSystemHeat** project. Output is written directly to:

- `GameData/zKerbalismSystemHeat/PluginData/zKerbalismSystemHeat.dll`

Intermediate files stay under `src/Core/obj` (not `bin/Release`).


## Optional patch

Optional patch in `Extras/SystemHeatFissionReactorsLowerMinThrust`: lowers fission reactor minimum throttle from 25% to 10%.

Copy the `SystemHeatFissionReactorsLowerMinThrust` folder into `GameData` to enable it.


## Mod settings

In `GameData/zKerbalismSystemHeat/Settings.cfg`:

* `BackgroundThermalSim` â€?enable simplified loop thermal simulation for unloaded vessels (default: `true`)
* `BackgroundRadiatorCoefficient` â€?scales radiator heat rejection in background thermal sim (default: `0.05`)


## Licensing

Licensed under the [MIT License](LICENSE).

Copyright (c) 2022 Alexander Rogov  
Copyright (c) 2026 Aebestach

Runtime dependencies (Kerbalism, SystemHeat, ModuleManager, HarmonyKSP, etc.) remain under their respective licenses and must be obtained separately.
