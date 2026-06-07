# Changelog

Notes for the **Kerbalism Bridge** monorepo. Each section is one installable mod under `GameData/`. Player-facing features, dependencies, and settings are listed here; version entries follow.

---

## Monorepo

Community fork of [judicator/KerbalismSystemHeat](https://github.com/judicator/KerbalismSystemHeat) and [judicator/KerbalismFFT](https://github.com/judicator/KerbalismFFT). Maintained at [Aebestach/KerbalismBridge](https://github.com/Aebestach/KerbalismBridge). Not an official judicator release.

### [1.0.0] - 2026-06-01

- Initial **Kerbalism Bridge** release: three main DLLs (`zKerbalismBridge`, `zKerbalismProcess`, `zKerbalismNative`) plus optional satellites.
- Process / Native architecture (Layer A / Layer B); see [docs/architecture/KerbalismBridge-Architecture-en.md](docs/architecture/KerbalismBridge-Architecture-en.md).
- Loaded via [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) (`PluginData/` + `*.host.xml`).
- Release packaging: `scripts/package-release.ps1`.

---

## zKerbalismBridge

**Main bridge trio** — install with `zKerbalismProcess` and `zKerbalismNative` for full SystemHeat integration.

Fork of [judicator/KerbalismSystemHeat](https://github.com/judicator/KerbalismSystemHeat). Integrates SystemHeat radiators, converters, harvesters, fission reactors, and fission engines with Kerbalism.

### Features

**Background (unloaded vessels)**

- Resource production/consumption for radiators, converters, harvesters, fission reactors, and fission engines.
- Optional **background thermal simulation** for SystemHeat loops (upstream 0.5.0 did not simulate loops in background).
- Fission reactors and trimodal fission engines run a full-throttle background model; manual throttle limits are respected when manual control is enabled.
- With **zKerbalismFFT** installed, fusion reactor and engine waste heat is included in background thermal sim.

**Active (loaded vessels)**

- Kerbalism resource accounting for the same part types. Fission reactors and engines adjust throttle to meet electricity demand via native logic while Kerbalism handles fuel and EC (upstream 0.5.0 mainly integrated unloaded vessels).
- Heat on loaded vessels still comes from native SystemHeat modules; only resource IO is routed through Kerbalism.

**Planner (VAB / Hangar)**

- Converters and harvesters scale with loop temperature and thermal efficiency.
- Editor auto-shutdown is disabled so parts keep running while the loop heats up; flight behaviour unchanged.
- Harvester “simulated resource abundance” remains available.

**Kerbalism Automation**

- Fission reactors and trimodal fission-engine reactors appear as scriptable devices when Automation is enabled.

**Supported third-party mods (SystemHeat patches)**

Atomic Age, Heat Control, Missing History, Near Future Aeronautics, Near Future Electrical (recycler via Process; capacitors via **zKerbalismNFE**), USI FTT, USI Core, Sterling Systems, Far Future Technologies (industrial plants via **zKerbalismFFT** + Process).

### Dependencies

- [Kerbalism](https://github.com/Kerbalism/Kerbalism) 3.32+
- [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost)
- [HarmonyKSP](https://github.com/KSPModdingLibs/HarmonyKSP) (via Kerbalism)
- [SystemHeat](https://github.com/post-kerbin-mining-corporation/SystemHeat) + **SystemHeatFissionReactors**, **SystemHeatFissionEngines**, **SystemHeatConverters**, **SystemHeatHarvesters** (from SystemHeat Extras)
- [Module Manager](https://github.com/sarbian/ModuleManager)

### Installation

Remove legacy `GameData/zKerbalismSystemHeat`, `GameData/zKerbalismNFE`, and any `Plugins/` copies. Install **zKerbalismBridge**, **zKerbalismProcess**, **zKerbalismNative**, and **zKerbalismPluginHost**. Do not place Bridge DLLs in `Plugins/`.

### Settings (`GameData/zKerbalismBridge/Settings.cfg`)

- `BackgroundThermalSim` — background loop thermal sim (default: `true`)
- `BackgroundRadiatorCoefficient` — radiator heat rejection scale in background sim (default: `0.05`)

### [1.0.0] - 2026-06-01

- Shared runtime: Harmony bootstrap, `SystemHeatBackgroundThermal`, editor sim, `BridgeSettings`.
- Localization keys `LOC_KerbalismBridge_*`.

---

## zKerbalismProcess

**Layer A (Process)** — Kerbalism `ProcessController` / `Harvester` integration with optional SystemHeat loop heat.

### Features

- `ProcessControllerSystemHeat`, `HarvesterSystemHeat`, converter / harvester / radiator Module Manager patches.
- Stock Kerbalism chemical plants, drills, and pumps; NFE processes on chemical plants; Sterling ISRU and fuel cells; FFT industrial converters and regolith scoops (with **zKerbalismFFT**).
- Requires **zKerbalismBridge**; SystemHeat patches additionally require SystemHeat.

### Dependencies

Same as main bridge; SystemHeat required for loop-heat patches.

### [1.0.0] - 2026-06-01

- Process layer: `ProcessControllerSystemHeat`, `HarvesterSystemHeat`, converter / harvester / radiator MM.
- Requires `zKerbalismBridge`; SystemHeat patches additionally require SystemHeat.

---

## zKerbalismNative

**Layer B core (Native)** — keeps mod-native modules; routes resources through Kerbalism via `*KerbalismUpdater` sidecars.

### Features

- Generic SystemHeat converter and harvester updaters.
- SystemHeat fission reactors and fission engines.
- NFE `nfe-reactor-*` excluded here (handled by Process Layer A). Other mods' fission parts use Native updaters.

### Dependencies

- Kerbalism, zKerbalismPluginHost, zKerbalismBridge, SystemHeat (+ fission extras), Module Manager, HarmonyKSP

### [1.0.0] - 2026-06-01

- Native layer: `*KerbalismUpdater`, SystemHeat fission.
- Requires `zKerbalismBridge`; per-mod patches declare additional `:NEEDS[...]`.

### [1.0.0] - 2026-06-02

- **Refactor:** Native is now **Layer B core only** (generic SH converters/harvesters, fission). NFE / SpaceDust / FFT regolith cleanup moved to satellites.

---

## zKerbalismNFE

**Layer B satellite** — [Near Future Electrical](https://github.com/post-kerbin-mining-corporation/NearFutureElectrical) discharge capacitors.

### Features

- Kerbalism EC integration for all `DischargeCapacitor` parts.
- Kerbalism Automation devices for NFE capacitors.

**NFE nuclear recycler (`nfe-nuclear-recycler-25-1`)** — **not** in this DLL. Uses **Layer A** via **zKerbalismProcess** (Kerbalism Configure + `ProcessControllerSystemHeat`). Legacy `nfe-nuclear-recycler-25` patches removed; use NFE 2.0+ recycler or Kerbalism ISRU chemical plants.

### Dependencies

- Kerbalism, zKerbalismPluginHost, zKerbalismBridge, **zKerbalismNative**, Near Future Electrical, Module Manager, HarmonyKSP
- Recycler also needs SystemHeat and **zKerbalismProcess**

### Installation

Copy `GameData/zKerbalismNFE` into KSP `GameData`. Requires the main bridge three-pack plus this satellite.

### [1.0.0] - 2026-06-02

- Restored as optional satellite (formerly merged into Native): NFE capacitors Layer B.

---

## zKerbalismSpaceDust

**Layer B satellite** — [SpaceDust](https://github.com/post-kerbin-mining-corporation/SpaceDust) harvesters (including FFT atmosphere / exosphere scoops when SpaceDust is installed).

### Features

- Loaded vessels: resource and ElectricCharge through Kerbalism; SpaceDust keeps sampling, intake physics, UI, and SystemHeat behaviour.
- Unloaded vessels: enabled harvesters are forced off; no background EC drain, SystemHeat flux, or resource harvest.

### Dependencies

- Kerbalism, zKerbalismPluginHost, zKerbalismBridge, **zKerbalismNative**, SystemHeat, SpaceDust, Module Manager, HarmonyKSP

### Installation

Copy `GameData/zKerbalismSpaceDust` into KSP `GameData`.

### [1.0.0] - 2026-06-02

- New satellite: SpaceDust `ModuleSpaceDustHarvester` Layer B integration.

---

## zKerbalismDynamicRadiation

Optional extension — **dynamic radiation decay** for nuclear and fusion parts integrated by **zKerbalismNative** and/or **zKerbalismFFT**.

### Features

- **Off / never started:** emitter disabled; radiation at configured minimum (% of peak).
- **Running:** full configured radiation.
- **After shutdown:** exponential decay toward minimum; works in flight and in background (unloaded vessel).
- Separate mod with no hard compile-time dependency on SystemHeat or FFT; tunable defaults.

### Dependencies

| Required | Optional (patches apply if present) |
|----------|-------------------------------------|
| Kerbalism + **FeatureRadiation** | **zKerbalismNative** — fission reactors/engines |
| zKerbalismPluginHost | **zKerbalismFFT** + Far Future Technologies — fusion reactors/engines, static-emitter FFT engines |
| Module Manager | |

### Installation

Copy `GameData/zKerbalismDynamicRadiation` into KSP `GameData`. Do not place the DLL in `Plugins/`.

### Settings (`GameData/zKerbalismDynamicRadiation/Settings.cfg`)

- `Reactor_MinEmissionPercent` / `Reactor_EmissionDecayRate`
- `Engine_MinEmissionPercent` / `Engine_EmissionDecayRate`
- Per-part overrides via `DynamicRadiationController` in custom MM patches.

### [1.0.0] - 2026-06-01

- Optional dynamic radiation decay for integrated SystemHeat fission and FFT fusion / static engine parts.
- No compile-time dependency on SystemHeat or FFT assemblies; tunable `Settings.cfg`.

---

## zKerbalismCryo

**Layer B satellite** — [CryoTanks](https://github.com/post-kerbin-mining-corporation/CryoTanks) Kerbalism integration.

### Features

**Classic `ModuleCryoTank` (EC cooling)**

- Flight and planner: cooling EC through Kerbalism (fixes dual EC drain with stock CryoTanks).
- Background: per-part boiloff and EC.
- Skips Kerbalism built-in `ProcessCryoTank` when the Cryo updater is present.

**`ModuleSystemHeatCryoTank` (SystemHeat path)**

- Background: simplified loop heating and boiloff when cooling is off or loop is too warm; optional integration with **zKerbalismBridge** background thermal sim.
- Loaded: native SystemHeat cryo simulation unchanged.
- Planner support for SH cryo tanks.

### Dependencies

- Kerbalism 3.32+, zKerbalismPluginHost, Module Manager, CryoTanks, SystemHeat (for SH cryo patches), HarmonyKSP (via Kerbalism)
- **Recommended:** zKerbalismBridge for full unloaded-vessel SystemHeat loop simulation

### Installation

Copy `GameData/zKerbalismCryo` into KSP `GameData`.

### Settings (`GameData/zKerbalismCryo/Settings.cfg`)

- `Enabled` — master switch (default `true`)

### [1.0.0] - 2026-06-02

- New satellite mod: CryoTanks `ModuleCryoTank` and SystemHeat `ModuleSystemHeatCryoTank` Kerbalism integration (Layer B).
- Fixes per-part background boiloff, Kerbalism EC path for active cooling, Harmony skip of duplicate `ProcessCryoTank`.

---

## zKerbalismFFT

Fork of [judicator/KerbalismFFT](https://github.com/judicator/KerbalismFFT). Kerbalism integration for [Far Future Technologies](https://github.com/post-kerbin-mining-corporation/FarFutureTechnologies).

### Features

**Antimatter tanks**

- Planner: EC for antimatter containment.
- Active and unloaded vessels: EC through Kerbalism. If containment loses power on an unloaded vessel, antimatter annihilates; thermal energy is applied on vessel load (can destroy the tank).

**Fusion reactors**

- Planner: EC production and De / De–He3 consumption; shows capacitor charging draw during startup.
- Loaded vessels: power and propellant through Kerbalism; throttle adjusts to meet demand (upstream 0.2.0 mainly integrated unloaded vessels).
- Unloaded vessels: full-power background model; capacitor charging simulated before startup.
- Fusion waste heat in **zKerbalismBridge** background thermal sim when the main bridge is installed.

**Fusion engines** (built-in reactor)

- Same Kerbalism routing and planner behaviour as standalone fusion reactors.

**Other FFT parts**

- Plain FFT engines: native propellant and thrust; SystemHeat engine modules, static radiation, dynamic radiation controllers, and reliability tuning remain.
- Chargeable engines: native capacitor charge state; Kerbalism limited to surrounding radiation/reliability patches.
- Particle detector science experiment → Kerbalism science (requires **FeatureScience**).
- Engine reliability patches (requires **FeatureReliability**).
- **CryoTanks:** use **zKerbalismCryo** (no longer shipped in this package).

**Industrial processors** (with **zKerbalismProcess**)

- Antimatter factory and nuclear smelter use Layer A `ProcessControllerSystemHeat`; all converter slots can run at once.

**Regolith scoops** (with **zKerbalismProcess**)

- Layer A `HarvesterSystemHeat`; background follows normal Kerbalism harvester path.

**Kerbalism Automation**

- Fusion reactors and fusion-engine reactors as scriptable devices when Automation is enabled.

**Kerbalism profile**

- Adds Antimatter, LqdDeuterium, and LqdHe3 supplies on KerbalismSupport profile.

### Dependencies

- Kerbalism 3.32+, zKerbalismPluginHost, HarmonyKSP, FarFutureTechnologies, SystemHeat
- **zKerbalismBridge** — strongly recommended (fusion waste heat in background thermal sim)
- Module Manager
- **zKerbalismCryo** — optional, for CryoTanks integration

### Installation

Remove any existing `GameData/zKerbalismFFT` before installing. Delete legacy `GameData/zKerbalismFFT/Plugin/` if upgrading from pre-PluginHost builds.

### Settings (`GameData/zKerbalismFFT/Settings.cfg`)

- `FFT_Engines_Radioactivity_Coeff` — static engine emitter scale (default `1.0`)
- `FFT_FusionReactors_Radioactivity_Coeff` — fusion reactor emitter scale (default `1.0`)
- `Antimatter_BackgroundDetonation` — annihilate antimatter in background after containment loss (default `true`)
- `Antimatter_DetonationGraceSeconds` — EC deficit before shutdown in background (default `0`)
- `Antimatter_MaxDetonationPerStep` — cap per background tick; `0` = unlimited (default `0`)

### [1.0.0] - 2026-06-01

- Kerbalism integration for Far Future Technologies: antimatter tanks, fusion reactors / engines, science, reliability.
- Loaded and unloaded vessel resource routing; optional background fusion heat bridge to `zKerbalismBridge`.
- Kerbalism Automation; KerbalismSupport profile supplies; B9PartSwitch antimatter tank patch; industrial Process + SystemHeat patches.

### [1.0.0] - 2026-06-02

- Antimatter background: fix EC deficit vs `elapsed_s` (false detonation).
- Settings: `Antimatter_BackgroundDetonation`, `Antimatter_DetonationGraceSeconds`, `Antimatter_MaxDetonationPerStep`.
- CryoTanks patches moved to **zKerbalismCryo**.
