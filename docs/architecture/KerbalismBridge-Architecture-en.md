# Kerbalism Bridge: Process Layer / Native Layer

**Kerbalism Bridge** bridges Kerbalism and third-party mods: **resources through Kerbalism**; **heat** through SystemHeat loops when needed, or through the mod's own native logic otherwise.

Integration uses **two layers** (legacy names: **Layer A = Process layer**, **Layer B = Native layer**). Pick one based on what module the part **originally** uses. Do not mix both on the same part.

---

## Packages and projects

Three DLLs form the main bridge; everything else is optional satellites. **SterlingSystemsKerbalism is not in this repo**—Sterling Systems maintains it separately.

```
KerbalismBridge/                         ← repo / solution
│
├── 【Main bridge · three DLLs】
│   GameData/zKerbalismBridge/           ← runtime (Harmony, background heat, editor sim)
│   GameData/zKerbalismProcess/          ← Process layer (ProcessControllerSystemHeat, etc.)
│   GameData/zKerbalismNative/           ← Native layer (*KerbalismUpdater, per-mod Harmony)
│
└── 【Satellites · optional】
    GameData/zKerbalismFFT/              ← FFT profile / industrial Process patches / Fusion MM
    GameData/zKerbalismDynamicRadiation/ ← separate DLL: post-shutdown radiation decay

【External · install separately, not merged into this repo】
    SterlingSystems                      ← parts
    SterlingSystemsKerbalism             ← Sterling Kerbalism profile + Process-stage cfg
```

| Package | DLL | Role |
|---------|-----|------|
| **zKerbalismBridge** | yes | Shared runtime; no Process / Updater modules |
| **zKerbalismProcess** | yes | Kerbalism **replacement** integration; `:NEEDS[zKerbalismBridge]`; heat patches also `:NEEDS[SystemHeat]` |
| **zKerbalismNative** | yes | **Keep native modules** + Updaters; `:NEEDS[zKerbalismBridge]`; per-mod patches (SystemHeat, FFT, NFE, …) |
| **zKerbalismFFT** | yes | Profile, industrial Process cfg, Fusion Updater MM |
| **zKerbalismDynamicRadiation** | yes | Optional gameplay; soft-deps on integrated reactors/engines |
| **SterlingSystemsKerbalism** | no | Maintained by Sterling; this repo only **`ModsSupport/SterlingSystems.cfg`** for FINAL heat bridge |

Dependencies:

```
Kerbalism
    └── zKerbalismBridge
            ├── zKerbalismProcess  ←── SystemHeat (optional, loop heat)
            └── zKerbalismNative   ←── SystemHeat / FFT / NFE … (per patch)
```

**NFE capacitor C#** is merged into **zKerbalismNative** (no standalone zKerbalismNFE.dll). FFT industrial plants, Sterling ISRU / **fuel cells**, etc. stay **Process layer** cfg.

---

## At a glance

| | **Process layer** (Layer A) | **Native layer** (Layer B) |
|---|----------------------------|----------------------------|
| **For** | Stock / Kerbalism-replaceable converters, harvesters, **fuel cells** | Mod **custom native** C# modules |
| **Approach** | Kerbalism `ProcessController` / `Harvester`; optionally upgrade to `*SystemHeat` | **Keep** native module; add `*KerbalismUpdater` sidecar |
| **Resources** | Kerbalism flow + brokers | Harmony blocks native IO; Kerbalism accounts |
| **Heat** | Optional: `ProcessControllerSystemHeat` + `ModuleSystemHeat` loop | With SH: native `UpdateFlux`, etc.; without SH: mod-owned (e.g. FFT fusion) |
| **Recipes** | Kerbalism Profile + Configure required | Usually **no** extra ISRU Profile |
| **SystemHeat** | **Optional** (Process + SH patches only if you want loop waste heat) | **Optional** (SH native modules use Updaters; Native works without SH too) |

---

## Process layer — Kerbalism replacement path

**Typical parts:** `ModuleResourceConverter`, `ModuleResourceHarvester` (Kerbalism chemical plants / drills / pumps, Sterling circular refineries, **metal fuel cells**, FFT industrial plants, etc.).

**Flow:**

1. MM converts the part to Kerbalism **`ProcessController`** / **`Harvester`** (+ Profile, Configure)
2. If SystemHeat is installed and loop heat is desired: `zKerbalismProcess` renames `ProcessController` → **`ProcessControllerSystemHeat`** (harvesters → **`HarvesterSystemHeat`**) and adds `ModuleSystemHeat`
3. Resources via Kerbalism; loop temperature affects efficiency when SH is enabled

**Examples:**

- Kerbalism default chemical plants / drills
- Sterling **`ConvertersMode0`** + **`Profile.cfg`** (SterlingSystemsKerbalism); this repo's **`SterlingSystems.cfg`** applies FINAL heat tuning
- Sterling **`SystemHeatFuelCells.cfg`**: `Configure title = Fuel Cell` → **Process layer**, not Native
- FFT industrial: `FFTIndustrialConverters.cfg` + `FarFutureTechnologies.cfg` (Process + SH)

**Fuel cells:** Same family as chemical plants—Kerbalism `ProcessController` + Fuel Cell-style `Configure`. Sterling MAEC uses the Process path; it does **not** keep `ModuleSystemHeatConverter` as a Native path.

---

## Native layer — native module sidecar

**Typical parts:** Mod-owned C# modules—`ModuleSystemHeatConverter`, `FusionReactor`, `DischargeCapacitor`, `ModuleSpaceDustHarvester`, etc.

**Flow:**

1. **Do not replace** the native C# module (UI, curves, mod logic unchanged)
2. Add a **`*KerbalismUpdater`** sidecar
3. Harmony **blocks** the native module's direct resource read/write
4. Heat: with SystemHeat, native `UpdateFlux()` etc.; without SH, stays in the mod (FFT fusion, NFE capacitors, …)

**Examples:**

- NFE nuclear recycler (`ModuleSystemHeatConverter` + Updater)
- FFT fusion reactors / fusion engines (`Fusion*KerbalismUpdater`, DLL in **zKerbalismNative** / **zKerbalismFFT**)
- NFE capacitors (`NFECapacitorKerbalismUpdater`, **zKerbalismNative**)
- SystemHeat fission reactors / engines, SpaceDust harvesters

---

## Which layer?

```
Part uses ModuleResourceConverter / ModuleResourceHarvester
(or a mod adapter pack already swapped it to ProcessController)
  → Process layer

Part uses a mod custom native module (ModuleSystemHeat*, FusionReactor, DischargeCapacitor, …)
  → Native layer (add Updater; do not replace with ProcessControllerSystemHeat)
```

---

## vs. legacy 0.5

Legacy **`SystemHeatConverterKerbalism`** replaced many parts that should have stayed on the Native layer.

**Bridge architecture:** Process layer uses `ProcessControllerSystemHeat` (optional SH); Native layer uses Updaters—**behaviour follows the mod author; resources follow Kerbalism**.

---

## C# namespaces

| Assembly | Namespace | Role |
|----------|-----------|------|
| zKerbalismBridge | `KerbalismBridge` | Runtime, background heat, editor sim |
| zKerbalismProcess | `KerbalismProcess` | ProcessControllerSystemHeat, HarvesterSystemHeat |
| zKerbalismNative | `KerbalismNative` | *KerbalismUpdater, NFE capacitors, fission Harmony |

Repository and solution: **`KerbalismBridge`** (`src/KerbalismBridge.sln`).
