# Changelog

Notes for the **KerbalismSystemHeatSupport** monorepo. Each section is one installable mod under `GameData/`.

---

## Monorepo

### [1.0.0] - 2026-05-31

- Initial **KerbalismSystemHeatSupport** release: four hosted Kerbalism plugins in one repository.
- Single solution builds `zKerbalismSystemHeat`, `zKerbalismFFT`, `zKerbalismNFE`, and `zKerbalismDynamicRadiation` DLLs.
- Loaded via [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) (`PluginData/` + `*.host.xml`).
- Release packaging: `scripts/package-release.ps1`.

---

## zKerbalismSystemHeat (1.0.0)

### [1.0.0] - 2026-05-31

- Kerbalism integration for SystemHeat: radiators, converters, harvesters, fission reactors/engines.
- `ProcessControllerSystemHeat` / `HarvesterSystemHeat`; loaded-vessel fission EC routing; `BackgroundThermalSim`.
- Kerbalism Planner and Automation support; mod compatibility patches; zh-cn / ru localization.
- B9PartSwitch and legacy third-party module name migration patches.

Upstream history (judicator fork): [docs/changelog/upstream-KerbalismSystemHeat.md](docs/changelog/upstream-KerbalismSystemHeat.md)

---

## zKerbalismFFT (1.0.0)

### [1.0.0] - 2026-05-31

- Kerbalism integration for Far Future Technologies: antimatter tanks, fusion reactors/engines, science, reliability.
- Loaded and unloaded vessel resource routing; optional background fusion heat bridge to zKerbalismSystemHeat.
- Kerbalism Automation; KerbalismSupport profile supplies; B9PartSwitch antimatter tank patch.

Upstream history (judicator fork): [docs/changelog/upstream-KerbalismFFT.md](docs/changelog/upstream-KerbalismFFT.md)

---

## zKerbalismNFE (1.0.0)

### [1.0.0] - 2026-05-31

- Kerbalism integration for NFE `DischargeCapacitor` parts (Planner, loaded/unloaded sim, Automation).
- Harmony patches block native NFE EC IO when updater is present; zh-cn localization.

---

## zKerbalismDynamicRadiation (1.0.0)

### [1.0.0] - 2026-05-31

- Optional dynamic radiation decay for integrated SystemHeat fission and FFT fusion/static engine parts.
- No compile-time dependency on SystemHeat or FFT assemblies; tunable `Settings.cfg`.

---

Dynamic radioactivity was removed from upstream judicator SystemHeat/FFT (FAR/Kopernicus) and is available again as optional **zKerbalismDynamicRadiation**.
