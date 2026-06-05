> Part of [Kerbalism Bridge](../../README.md). Build: `src/KerbalismBridge.sln` (project **zKerbalismCryo**).

# zKerbalismCryo

**Version:** 1.0.0

Kerbalism resource integration for [CryoTanks](https://github.com/post-kerbin-mining-corporation/CryoTanks): classic `ModuleCryoTank` (EC cooling) and **SystemHeat** `ModuleSystemHeatCryoTank` (loop heat + background boiloff).

## Features

### ModuleCryoTank (EC path)

- Flight and planner: cooling EC through Kerbalism (`ResourceCache`), fixing dual EC drain with stock CryoTanks ([Kerbalism #717](https://github.com/Kerbalism/Kerbalism/issues/717)).
- Background: per-part boiloff and EC (does not drain the same resource from all tanks on the vessel).
- Skips Kerbalism built-in `ProcessCryoTank` when `CryoTankKerbalismUpdater` is present.

### ModuleSystemHeatCryoTank (SystemHeat path)

- Background: simplified loop heating, boiloff when cooling is off or loop is too warm; optional call into `zKerbalismBridge` `SystemHeatBackgroundThermal` when installed.
- Loaded: native SystemHeat cryo simulation unchanged.
- Planner: `PlannerController` entry for SH cryo tanks.

## Dependencies

- [Kerbalism](https://github.com/Kerbalism/Kerbalism) 3.32+
- [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost)
- [Module Manager](https://github.com/sarbian/ModuleManager)
- [CryoTanks](https://github.com/post-kerbin-mining-corporation/CryoTanks)
- [SystemHeat](https://github.com/post-kerbin-mining-corporation/SystemHeat) — required for SH cryo tank patches
- [HarmonyKSP](https://github.com/KSPModdingLibs/HarmonyKSP) — via Kerbalism
- **Recommended:** [zKerbalismBridge](https://github.com/Aebestach/KerbalismBridge) for full unloaded-vessel SystemHeat loop simulation

## Installation

Copy `GameData/zKerbalismCryo` into KSP `GameData`. Load via **zKerbalismPluginHost** (`PluginData/zKerbalismCryo.dll`).

## Settings

`GameData/zKerbalismCryo/Settings.cfg`:

- `Enabled` — master switch (default `true`)
