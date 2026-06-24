# Changelog

Version history for the **Kerbalism Bridge** monorepo. Each section is one installable mod under `GameData/`.

Installation, dependencies, and package overview: [README.md](README.md) · [README-CN.md](README-CN.md)

---

## Monorepo

Community fork of [judicator/KerbalismSystemHeat](https://github.com/judicator/KerbalismSystemHeat) and [judicator/KerbalismFFT](https://github.com/judicator/KerbalismFFT). Maintained at [Aebestach/KerbalismBridge](https://github.com/Aebestach/KerbalismBridge). Not an official judicator release.

### [1.0.6] - 2026-06-24

- **Fix:** SystemHeat background loop thermal sim (**zKerbalismBridge**): capture NFE fission reactor running state when individual parts pack and snapshot loop temperatures when switching away from a loaded vessel; background automation and thermal sim no longer lose reactor/throttle state after vessel switch.

### [1.0.5] - 2026-06-23

- **Fix:** High timewarp false EC / input shutdown across Layer B modules: CryoTanks active cooling, FFT fusion reactor/engine charging, NFE discharge capacitors, and SpaceDust harvesters now pre-check per-second EC instead of `rate × fixedDeltaTime` / `elapsed_s`.
- **Fix:** FFT antimatter tanks (**zKerbalismFFT**): background containment no longer misjudges EC deficit; loaded state avoids double EC draw (Harmony skip of native `ConsumeCharge` / `DoCatchup` when `FFTModuleAntimatterTankKerbalism` owns the tank).
- **Fix:** SystemHeat native converter/harvester (**zKerbalismNative**): temporarily zero stock `ModuleResourceConverter` input ratios during `FixedUpdateFlight` while Kerbalism owns resource IO, preventing high timewarp input-validation shutdown.
- **Fix:** SpaceDust exosphere harvesters (e.g. PK-EXO Bussard collector) (**zKerbalismSpaceDust**): Kerbalism background sim now harvests on unload — sync native `Enabled` to proto while loaded, fall back to `orbit.vel` when `obt_velocity` is zero, assume ideal intake alignment (`dot = 1`), and skip atmospheric ram scoops in background (loaded flight only).
- **Fix:** SpaceDust loaded harvest UI sync after native `FixedUpdate` when Kerbalism blocks resource IO.

### [1.0.4] - 2026-06-22

- **Enhancement:** SystemHeat background loop thermal sim (**zKerbalismBridge**): capture loaded NFE fission reactor state on scene switch/pause; integrate loop temperatures with flux anchors and radiator temperature curves; default `BackgroundRadiatorCoefficient` raised from `0.05` to `1.0`.
- **Refactor:** CryoTanks background access (**zKerbalismCryo**): route boiloff and EC paths through SimpleBoiloff helpers and shared access types for cleaner bridge integration.
- **Enhancement:** SpaceDust Layer B background harvest (**zKerbalismSpaceDust**): Kerbalism background resource sim now routes harvest rates and EC for unloaded vessels with `SpaceDustHarvesterKerbalismUpdater`.
- **Feature:** Kerbalism Automation devices for generic SystemHeat native modules (**zKerbalismNative**) and SpaceDust harvesters (**zKerbalismSpaceDust**); FFT atmosphere scoop excluded from automation list.
- **Fix:** SpaceDust harvester thermal efficiency clamped to 0–1 so Kerbalism rates never exceed nominal when the SystemHeat curve evaluates above unity.
- **Fix:** SpaceDust resource blocking uses Harmony `Part.RequestResource` patches (all overloads) instead of stack-trace matching.
- **Fix:** SpaceDust harvesters with Kerbalism Layer B (**zKerbalismSpaceDust**): stop clearing `Enabled` when blocking native `SpaceDustHarvesterBackground.Process`; background sim no longer turns off harvesters (e.g. PK-EXO Bussard collector) when the vessel unloads.

### [1.0.3] - 2026-06-10

- **Fix:** SystemHeat 0.7+ / 0.9.x compatibility (**zKerbalismNative**): update Harmony `PostProcess` prefix patches for `ModuleSystemHeatConverter` and `ModuleSystemHeatHarvester` to match the current `(ConverterResults, double deltaTime)` signature (fixes `KerbalismNative load failed` / `Undefined target method` on startup).
- **Fix:** SystemHeat radiator + TweakScale compatibility (**zKerbalismNative**, **zKerbalismProcess**): stop PartLoader `OnLoad` NRE when caching the prefab temperature curve; avoid empty `resHandler.inputResources` during `FixedUpdate` (fixes `IndexOutOfRangeException` in `UpdatePAW` / `FixedUpdate` when scaling radiators in the editor).
- **Fix:** add `ElectricCharge` `RESOURCE` to stock `ModuleActiveRadiator` → `SystemHeatRadiatorKerbalism` conversions so SystemHeat UI and sim have a valid input resource list.
- **Fix:** remove invalid TweakScale `RESOURCE` exponent for `SystemHeatRadiatorKerbalism` (EC scaling stays in code via `scale` / `scaleEmissionPower`).
- **Balance:** CRANE particle detector science (**zKerbalismFFT**): replace `Surface@Biomes` with global `SrfLanded` / `SrfSplashed` in `FFTScience.cfg`; keep `InSpaceLow` / `InSpaceHigh`.

### [1.0.2] - 2026-06-08

- Fix FFT regolith scoop Kerbalism harvester balance (**zKerbalismFFT**): preserve FFT `HarvestThreshold`, `Efficiency`, and thermal params instead of generic drill thresholds/rates.
- **Fix:** restore Kerbalism dump-valve status text on Layer A converters and reactors (**zKerbalismProcess**); PAW again shows which outputs are vented (e.g. `Dump: Nothing`, `Dump: Oxygen`).
- **Fix:** restore manual fission-reactor power throttle in PAW for NFE Layer A reactors (**zKerbalismProcess**); `CurrentPowerPercent` slider drives EC, waste heat, and Planner while running.
- **Fix:** NFE Layer A fission reactors default dump valve to `ElectricCharge` (**zKerbalismProcess**); Planner and Monitor no longer treat onboard EC buffer as the generation cap when Kerbalism profile lists EC as a vent option.

### [1.0.1] - 2026-06-08

- Maintenance release: fix TweakScale-scaled SystemHeat radiators losing all heat rejection (**zKerbalismNative**).

### [1.0.0] - 2026-06-07

- Initial **Kerbalism Bridge** release: three main DLLs (`zKerbalismBridge`, `zKerbalismProcess`, `zKerbalismNative`) plus optional satellites.
- Process / Native architecture (Layer A / Layer B); see [docs/architecture/KerbalismBridge-Architecture-en.md](docs/architecture/KerbalismBridge-Architecture-en.md).
- Loaded via [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) (`PluginData/` + `*.host.xml`).
- Release packaging: `scripts/package-release.ps1`.

---

## zKerbalismBridge

### [1.0.6] - 2026-06-24

- **Fix:** `KerbalismBridgeCoreInit` hooks `onPartPack`, `onVesselSwitching`, and `onVesselSwitchingToUnloaded` to call `CaptureLoadedFissionReactorState` per part and `CaptureLoadedTemperatures` on the departing vessel before unload.
- **Fix:** `CaptureLoadedFissionReactorState` is public with `Enabled` / null guards so part-pack capture can reuse the same proto sync path as scene-switch capture.

### [1.0.4] - 2026-06-22

- **Enhancement:** `SystemHeatBackgroundThermal` refactor: snapshot loaded NFE fission reactor state on scene switch/pause; loop temperatures integrate flux anchors and radiator temperature curves for more accurate unloaded-vessel thermal sim.
- **Change:** default `BackgroundRadiatorCoefficient` in `Settings.cfg` raised from `0.05` to `1.0` (fallback rejection when no temperature curve is available).

### [1.0.0] - 2026-06-07

- Shared runtime: Harmony bootstrap, `SystemHeatBackgroundThermal`, editor sim, `BridgeSettings`.
- Background resource sim and optional loop thermal sim for unloaded vessels; loaded-vessel resource IO for SystemHeat radiators, converters, harvesters, fission reactors, and fission engines.
- Planner support; Kerbalism Automation devices for fission parts.
- Fusion waste heat in background thermal sim when **zKerbalismFFT** is installed.
- Localization keys `LOC_KerbalismBridge_*`.

---

## zKerbalismProcess

### [1.0.3] - 2026-06-10

- **Fix:** `SystemHeatRadiators.cfg` adds `ElectricCharge` `RESOURCE` when converting stock `ModuleActiveRadiator` parts (SystemHeat expects at least one module input resource).
- **Fix:** drop invalid `RESOURCE` block from `TweakScale/ScaleExponents.cfg` for `SystemHeatRadiatorKerbalism`.

### [1.0.2] - 2026-06-08

- **Fix:** `ProcessControllerSystemHeat` and `ProcessControllerDeployable` PAW dump button shows active valve title again (`ProcessControllerUiHelper`); matches stock Kerbalism `Dump: <mode>` labeling.
- **Fix:** NFE `ProcessControllerSystemHeat` fission reactors (`_Nukereactor`) expose `CurrentPowerPercent` throttle slider in the fission-reactor PAW group; respects per-part `MinimumThrottle` and updates Kerbalism pseudo-resource throughput plus SystemHeat flux.
- **Fix:** set `valve_i = 1` on NFE fission reactors so the default dump mode is `ElectricCharge` (requires Kerbalism `fission reactor` profile to list EC in `dump_valve`).

### [1.0.0] - 2026-06-07

- Layer A: `ProcessControllerSystemHeat`, `HarvesterSystemHeat`, converter / harvester / radiator Module Manager patches.
- Stock Kerbalism plants, drills, and pumps; NFE processes; Sterling ISRU and fuel cells; FFT industrial converters and regolith scoops (with **zKerbalismFFT**).

---

## zKerbalismNative

### [1.0.5] - 2026-06-23

- **Fix:** `SHNativeConverterInputHarmony` — while Kerbalism Layer B owns SH native converter/harvester resource IO, zero `inputList` ratios for the stock `ModuleResourceConverter` input check during `FixedUpdateFlight` and restore afterward; prevents high timewarp false “insufficient input” shutdown.

### [1.0.4] - 2026-06-22

- **Feature:** Kerbalism Automation devices for generic SystemHeat Layer B modules (`SystemHeatNativeModuleDevices`, `SHNativeDeviceCollector`, `ComputerDevicesSHNativePatch`).

### [1.0.3] - 2026-06-10

- **Fix:** `Patch_SystemHeatConverter_PostProcess` and `Patch_SystemHeatHarvester_PostProcess` Harmony prefixes now include `double deltaTime`, matching SystemHeat 0.7+ (`PostProcess(ConverterResults, double)`); restores Native load with SystemHeat 0.9.x.
- **Fix:** `SystemHeatRadiatorKerbalism.EnsureBaseTemperatureCurve` no longer runs in `OnLoad` and guards null `partPrefab` / `FloatCurve.Curve` (eliminates PartLoader NRE on Heat Control and other converted radiators).
- **Fix:** `FixedUpdate` zeroes module resource rates instead of replacing `resHandler.inputResources` with an empty list, so SystemHeat base `UpdatePAW` / `FixedUpdate` no longer throw when TweakScale rescales a radiator.

### [1.0.1] - 2026-06-08

- **Fix:** `SystemHeatRadiatorKerbalism.OnPartScaleChanged` no longer wipes `temperatureCurve` when TweakScale rescales a radiator; heat rejection is rebuilt from the part prefab curve using the absolute scale factor.
- **Fix:** background EC scaling for scaled radiators reads `scaleEmissionPower` (was `scaleECConsumptionPower`).
- Scaled radiators reload with a valid curve in `OnStart` when `scale != 1`.

### [1.0.0] - 2026-06-07

- Layer B core: `*KerbalismUpdater`, generic SystemHeat converters/harvesters, SystemHeat fission reactors and engines.
- **Refactor:** Native is now Layer B core only. NFE / SpaceDust / FFT regolith cleanup moved to satellites.

---

## zKerbalismNFE

### [1.0.5] - 2026-06-23

- **Fix:** NFE discharge capacitor Layer B: loaded and background charging use per-second EC pre-check instead of whole physics-step / elapsed-time totals (high timewarp false power-off).

### [1.0.0] - 2026-06-07

- Restored as optional satellite: NFE discharge capacitors (Layer B) and Kerbalism Automation devices.
- NFE nuclear recycler (`nfe-nuclear-recycler-25-1`) uses Layer A via **zKerbalismProcess**; legacy `nfe-nuclear-recycler-25` patches removed.

---

## zKerbalismSpaceDust

### [1.0.5] - 2026-06-23

- **Fix:** Exosphere harvesters (PK-EXO): background resource sim produces harvest rates on unload — `SyncProtoState` writes native `Enabled` to proto while loaded; `GetExosphereOrbitalVelocity` uses `orbit.vel` when `obt_velocity` is zero; background assumes ideal intake alignment (`dot = 1`).
- **Fix:** `IsHarvesterEnabledInProto` and `HasBackgroundOperatingPower` for robust proto `Enabled` parsing and EC gating in background.
- **Change:** `HarvestType.Atmosphere` harvesters are skipped in Kerbalism background sim (must fly loaded in atmosphere; avoids misleading on-rails production).
- **Fix:** loaded EC pre-check and native UI sync (`HasOperatingPower`, `SyncNativeUiAfterFixedUpdate`) aligned with Kerbalism high-timewarp behavior.

### [1.0.4] - 2026-06-22

- **Enhancement:** Kerbalism background resource sim routes SpaceDust harvest rates and EC for unloaded vessels (`AddBackgroundHarvestRates`, thermal scale from linked SystemHeat loop).
- **Feature:** Kerbalism Automation devices for SpaceDust harvesters (`SpaceDustHarvesterDevices`, `SpaceDustDeviceCollector`); FFT atmosphere scoop excluded.
- **Fix:** clamp `SystemEfficiency` thermal scale to 0–1 for loaded and background harvest accounting.
- **Fix:** replace stack-trace `RequestResource` blocking with Harmony patches on all `Part.RequestResource` overloads (`SpaceDustResourceBlocker`).
- **Fix:** `SpaceDustBackgroundProcessPrefix` no longer sets harvester `Enabled` to false when skipping native background processing; matches Kerbalism behavior so Layer B harvesters stay on during Kerbalism background resource sim.

### [1.0.0] - 2026-06-07

- New satellite: SpaceDust `ModuleSpaceDustHarvester` Layer B integration.
- Loaded vessels: Kerbalism resource and EC routing; unloaded vessels: harvesters forced off (no background drain).

---

## zKerbalismDynamicRadiation

### [1.0.0] - 2026-06-07

- Optional dynamic radiation decay for integrated SystemHeat fission and FFT fusion / static engine parts.
- No compile-time dependency on SystemHeat or FFT assemblies; tunable `Settings.cfg`.

---

## zKerbalismCryo

### [1.0.5] - 2026-06-23

- **Fix:** CryoTanks Layer B active cooling: loaded and background EC checks use per-second rate; background no longer multiplies EC threshold by `elapsed_s`; removed duplicate loaded `FixedUpdate` path.

### [1.0.4] - 2026-06-22

- **Refactor:** CryoTanks and SystemHeat cryo-tank access routed through SimpleBoiloff helpers (`CryoTankAccess`, `SystemHeatCryoTankAccess`); shared boiloff/EC logic for background bridge integration.

### [1.0.0] - 2026-06-07

- New satellite: CryoTanks `ModuleCryoTank` and SystemHeat `ModuleSystemHeatCryoTank` Kerbalism integration (Layer B).
- Fixes per-part background boiloff, Kerbalism EC path for active cooling, Harmony skip of duplicate `ProcessCryoTank`.

---

## zKerbalismFFT

### [1.0.5] - 2026-06-23

- **Fix:** FFT antimatter tank Layer B (`FFTModuleAntimatterTankKerbalism`): background containment EC check uses per-second rate; loaded state uses `SetPoweredState` + capped charge request; Harmony skips native `ModuleAntimatterTank.ConsumeCharge` and `DoCatchup` to prevent double EC deduction.
- **Fix:** FFT fusion reactor/engine Layer B charging: per-second EC pre-check for loaded and background sim (high timewarp false “insufficient power” during charge).

### [1.0.3] - 2026-06-10

- **Balance:** CRANE particle detector (`fftParticleDetector`) in `FFTScience.cfg`: replace `Surface@Biomes` with global `SrfLanded` / `SrfSplashed` (one landed subject per body; splashed only on ocean bodies). Keep `InSpaceLow` / `InSpaceHigh`. Removes per-biome surface subjects that inflated science yield (e.g. Mun).

### [1.0.2] - 2026-06-08

- **Fix:** `FFT_Regoliths_SystemHeat.cfg` maps FFT `HarvestThreshold` → `min_abundance`, `Efficiency` → `rate`, and sets `abundance_rate = 1`; inherits `systemPower` and loop temperature limits from the stock FFT harvester. Restores trace-resource harvesting (e.g. Mun LqdHe3) blocked by the previous 2% drill threshold and nerfed `0.0025` rate template.

### [1.0.0] - 2026-06-07

- Kerbalism integration for Far Future Technologies: antimatter tanks, fusion reactors / engines, science, reliability.
- Loaded and unloaded vessel resource routing; optional background fusion heat bridge to `zKerbalismBridge`.
- Kerbalism Automation; KerbalismSupport profile supplies; B9PartSwitch antimatter tank patch; industrial Process + SystemHeat patches.
- Antimatter background: fix EC deficit vs `elapsed_s` (false detonation).
- Settings: `Antimatter_BackgroundDetonation`, `Antimatter_DetonationGraceSeconds`, `Antimatter_MaxDetonationPerStep`.
- CryoTanks patches moved to **zKerbalismCryo**.

---

## zKerbalismSterlingSystems

### [1.0.0] - 2026-06-07

- Optional satellite: Module Manager patches for Sterling Systems fission, converters, engines, radiators, fuel cells, and related parts.
- Config-only (no DLL); requires **zKerbalismBridge**, **zKerbalismProcess**, and Sterling Systems.
- Maintained in-repo; successor to SterlingSystemsKerbalism.
