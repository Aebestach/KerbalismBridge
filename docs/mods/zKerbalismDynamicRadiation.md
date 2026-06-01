> Part of [Kerbalism Bridge](../../README.md). Build: `src/KerbalismBridge.sln`.
# zKerbalismDynamicRadiation

**Version:** 1.0.0

Optional Kerbalism extension that restores **dynamic radiation** for nuclear and fusion parts integrated by [zKerbalismNative](https://github.com/Aebestach/KerbalismBridge) and/or [zKerbalismFFT](https://github.com/Aebestach/KerbalismBridge).

## Behavior

- **Off / never started:** primary `Emitter` is disabled; radiation at configured minimum (% of peak).
- **Running:** emitter on at full configured `radiation` (from Kerbalism / NFE / FFT patches).
- **After shutdown:** exponential decay toward minimum; works in flight and in background (unloaded vessel).

Improvements over older monolithic Kerbalism?SystemHeat integrations:

- Separate mod ??no bootstrap / hard dependency on SystemHeat or FFT assemblies.
- Picks the **highest positive** `Emitter` on the part (avoids `First()` on shield emitters).
- Matches power module by `moduleID` / `ModuleID` when multiple reactors/engines exist.
- Tunable defaults in `Settings.cfg`.

## Requirements

| Required | Optional (patches apply only if folder present) |
|----------|--------------------------------------------------|
| Kerbalism + **FeatureRadiation** | `zKerbalismNative` ? fission reactors/engines |
| [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) | `zKerbalismFFT` + Far Future Technologies ? fusion reactors/engines, static-emitter FFT rocket engines |
| Module Manager | |

## Install

Copy `GameData/zKerbalismDynamicRadiation` into KSP `GameData` and install **zKerbalismPluginHost** separately. Build the DLL with Visual Studio or:

```text
msbuild src\KerbalismBridge.sln /p:Configuration=Release
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
