# Changelog

Notes for the **Kerbalism Bridge** monorepo. Each section is one installable mod under `GameData/`.

---

## Monorepo

### [1.0.0] - 2026-06-01

- Initial **Kerbalism Bridge** release: three main DLLs (`zKerbalismBridge`, `zKerbalismProcess`, `zKerbalismNative`) plus optional satellites (`zKerbalismFFT`, `zKerbalismDynamicRadiation`, `zKerbalismResourceAudit`).
- Process / Native architecture; see [docs/architecture/KerbalismBridge-Architecture.md](docs/architecture/KerbalismBridge-Architecture.md).
- NFE capacitor integration ships in **zKerbalismNative** (no separate NFE package).
- Loaded via [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) (`PluginData/` + `*.host.xml`).
- Release packaging: `scripts/package-release.ps1`.

---

## zKerbalismBridge (1.0.0)

### [1.0.0] - 2026-06-01

- Shared runtime: Harmony bootstrap, `SystemHeatBackgroundThermal`, editor sim, `BridgeSettings`.
- Localization keys `LOC_KerbalismBridge_*`.

---

## zKerbalismProcess (1.0.0)

### [1.0.0] - 2026-06-01

- Process layer: `ProcessControllerSystemHeat`, `HarvesterSystemHeat`, converter / harvester / radiator MM.
- Requires `zKerbalismBridge`; SystemHeat patches additionally require SystemHeat.

---

## zKerbalismNative (1.0.0)

### [1.0.0] - 2026-06-01

- Native layer: `*KerbalismUpdater`, SystemHeat fission, SpaceDust, NFE recycler and capacitor integration.
- Requires `zKerbalismBridge`; per-mod patches declare additional `:NEEDS[...]`.

---

## zKerbalismFFT (1.0.0)

### [1.0.0] - 2026-06-01

- Kerbalism integration for Far Future Technologies: antimatter tanks, fusion reactors / engines, science, reliability.
- Loaded and unloaded vessel resource routing; optional background fusion heat bridge to `zKerbalismBridge`.
- Kerbalism Automation; KerbalismSupport profile supplies; B9PartSwitch antimatter tank patch; industrial Process + SystemHeat patches.

---

## zKerbalismDynamicRadiation (1.0.0)

### [1.0.0] - 2026-06-01

- Optional dynamic radiation decay for integrated SystemHeat fission and FFT fusion / static engine parts.
- No compile-time dependency on SystemHeat or FFT assemblies; tunable `Settings.cfg`.

---

## zKerbalismResourceAudit (1.0.0)

### [1.0.0] - 2026-06-02

- Optional static audit of loaded parts with stock/native resource modules not integrated with Kerbalism/Bridge.
- Writes English logs under `Logs/zKerbalismResourceAudit/scan_*` (summary, parts, by-mod, by-module).
- Tunable `Settings.cfg`; no in-flight scanning.
