> Part of [KerbalismSystemHeatSupport](../../README.md). Build: `src/KerbalismSystemHeatSupport.sln`.
# zKerbalismDynamicRadiation

Optional Kerbalism extension that restores **dynamic radiation** for nuclear and fusion parts integrated by [zKerbalismSystemHeat](https://github.com/Aebestach/KerbalismSystemHeat) and/or [zKerbalismFFT](https://github.com/Aebestach/KerbalismFFT).

## Behavior

- **Off / never started:** primary `Emitter` is disabled; radiation at configured minimum (% of peak).
- **Running:** emitter on at full configured `radiation` (from Kerbalism / NFE / FFT patches).
- **After shutdown:** exponential decay toward minimum; works in flight and in background (unloaded vessel).

Improvements over KerbalismSystemHeat 0.4.0:

- Separate mod â€?no bootstrap / hard dependency on SystemHeat or FFT assemblies.
- Picks the **highest positive** `Emitter` on the part (avoids `First()` on shield emitters).
- Matches power module by `moduleID` / `ModuleID` when multiple reactors/engines exist.
- Tunable defaults in `Settings.cfg`.

## Requirements

| Required | Optional (patches apply only if folder present) |
|----------|--------------------------------------------------|
| Kerbalism + **FeatureRadiation** | `zKerbalismSystemHeat` â€?fission reactors/engines |
| [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) | `zKerbalismFFT` + Far Future Technologies â€?fusion reactors/engines, static-emitter FFT rocket engines |
| Module Manager | |

## Install

Copy `GameData/zKerbalismDynamicRadiation` into KSP `GameData` and install **zKerbalismPluginHost** separately. Build the DLL with Visual Studio or:

```text
msbuild src\KerbalismSystemHeatSupport.sln /p:Configuration=Release
```

Output:

- `GameData/zKerbalismDynamicRadiation/PluginData/zKerbalismDynamicRadiation.dll`
- `GameData/zKerbalismDynamicRadiation/zKerbalismDynamicRadiation.host.xml`

Do not place the DLL in `Plugins`, or KSP will load it before Kerbalism Bootstrap and fail.

## Settings

Edit `GameData/zKerbalismDynamicRadiation/Settings.cfg`:

- `Reactor_MinEmissionPercent` / `Reactor_EmissionDecayRate`
- `Engine_MinEmissionPercent` / `Engine_EmissionDecayRate`

Per-part overrides: set `minEmissionPercent` and `emissionDecayRate` on the `DynamicRadiationController` module in a custom MM patch.
