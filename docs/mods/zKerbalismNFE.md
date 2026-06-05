> Part of [Kerbalism Bridge](../../README.md). Build: `src/KerbalismBridge.sln` (project **zKerbalismNFE**).

# zKerbalismNFE

**Version:** 1.0.0

Layer B (Native) satellite for [Near Future Electrical](https://github.com/post-kerbin-mining-corporation/NearFutureElectrical): discharge capacitors.

## Features

- `NFECapacitorKerbalismUpdater` on all `DischargeCapacitor` parts (Kerbalism EC, Harmony blocks native IO).
- Kerbalism Automation devices for NFE capacitors.

## NFE nuclear recycler (`nfe-nuclear-recycler-25-1`)

**Not** in this DLL. NFE 2.0+ recycler uses **Layer A** (Kerbalism `Configure` + `ProcessControllerSystemHeat`):

- Profile processes in `KerbalismConfig/Support/NFElectric.cfg` (`_NfeDepletedReprocess`, `_NfeXeExtract`, `_NfeOreRefine`) — base rates are **0.1×** stock NFE converter ratios; `nfe-nuclear-recycler-25-1` uses `capacity = 10` to match stock throughput. Same three processes are available on Kerbalism ISRU chemical plants (scaled capacity like other ISRU paths).
- Part patch: `Configure` **Nuclear Processor**, `slots = 3` on `nfe-nuclear-recycler-25-1`
- Layer A guard: `GameData/zKerbalismNFE/Patches/NFERecycler_LayerA.cfg`
- SystemHeat: `zKerbalismProcess/Patches/NFERecycler_ProcessControllerSystemHeat.cfg` (`ProcessControllerSystemHeat`, loop `isru`, systemPower 175 / 75 / 175 kW)

Legacy `nfe-nuclear-recycler-25` centrifuge/breeder Kerbalism patches were removed; use NFE 2.0+ `nfe-nuclear-recycler-25-1` or ISRU plants.

## Dependencies

- Kerbalism, zKerbalismPluginHost, zKerbalismBridge, **zKerbalismNative**, Near Future Electrical, Module Manager, HarmonyKSP.
- Recycler also needs **SystemHeat** and **zKerbalismProcess**.

## Installation

Copy `GameData/zKerbalismNFE` into KSP `GameData`. Requires the main bridge three-pack plus this satellite.
