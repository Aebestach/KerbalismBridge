> Part of [Kerbalism Bridge](../../README.md). Build: `src/KerbalismBridge.sln` (project **zKerbalismSpaceDust**).

# zKerbalismSpaceDust

**Version:** 1.0.0

Layer B (Native) satellite for [SpaceDust](https://github.com/post-kerbin-mining-corporation/SpaceDust) `ModuleSpaceDustHarvester` parts (including FFT atmosphere and exosphere scoops when SpaceDust is installed).

## Features

- `SpaceDustHarvesterKerbalismUpdater` + Harmony resource blocking on native harvester `FixedUpdate`.
- Loaded-vessel resource and ElectricCharge accounting through Kerbalism while SpaceDust keeps sampling, intake physics, UI, and SystemHeat behaviour.
- When a vessel becomes unloaded, enabled SpaceDust harvesters are forced off. Background simulation does not consume ElectricCharge, emit SystemHeat flux, or harvest resources for these parts.

`PowerCost` is treated as ElectricCharge per second, matching KSP resource units despite SpaceDust UI text describing it as kW.

## Dependencies

- Kerbalism, zKerbalismPluginHost, zKerbalismBridge, **zKerbalismNative**, SystemHeat, SpaceDust, Module Manager, HarmonyKSP.

## Installation

Copy `GameData/zKerbalismSpaceDust` into KSP `GameData`.
