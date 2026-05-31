# KerbalismSystemHeatSupport

Kerbalism hosted support for **SystemHeat**, **Near Future Electrical**, **Far Future Technologies**, and optional **dynamic radiation** — four separate KSP mods, one development repository.

**Version:** 1.0.0 (mod family release)

All plugins load through [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) after Kerbalism is present. Each mod remains a **separate install**; players only need the folders they use.

## Included mods

| Mod folder | DLL | Purpose |
|------------|-----|---------|
| `GameData/zKerbalismSystemHeat` | `zKerbalismSystemHeat.dll` | Kerbalism resource / planner / background integration for [SystemHeat](https://github.com/post-kerbin-mining-corporation/SystemHeat) |
| `GameData/zKerbalismFFT` | `zKerbalismFFT.dll` | Kerbalism integration for [Far Future Technologies](https://github.com/post-kerbin-mining-corporation/FarFutureTechnologies) |
| `GameData/zKerbalismNFE` | `zKerbalismNFE.dll` | Kerbalism integration for NFE `DischargeCapacitor` parts |
| `GameData/zKerbalismDynamicRadiation` | `zKerbalismDynamicRadiation.dll` | Optional dynamic radiation decay for integrated fission/fusion parts |

Detailed per-mod documentation: [docs/mods/](docs/mods/)

## Common requirements

| Required for all | Notes |
|------------------|-------|
| [Kerbalism](https://github.com/Kerbalism/Kerbalism) 3.32+ | Bootstrap `*.kbin` workflow |
| [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) | Deferred loader; **do not** put these DLLs in `Plugins/` |
| [Module Manager](https://github.com/sarbian/ModuleManager) | Patches |

Additional dependencies are per mod (SystemHeat, FFT, NFE, HarmonyKSP, etc.) — see each mod's doc under [docs/mods/](docs/mods/).

## Recommended install set

Typical SystemHeat + NF stack:

1. Kerbalism + zKerbalismPluginHost + HarmonyKSP  
2. SystemHeat (+ fission/converter/harvester extras as needed)  
3. **zKerbalismSystemHeat**  
4. Near Future Electrical → **zKerbalismNFE** (capacitors)  
5. Far Future Technologies → **zKerbalismFFT** (strongly pair with SystemHeat for fusion background heat)  
6. **zKerbalismDynamicRadiation** (optional; needs SystemHeat and/or FFT integration patches)

Copy only the `GameData/zKerbalism*` folders you need into KSP `GameData/`.

## Building

Open `src/KerbalismSystemHeatSupport.sln` in Visual Studio and **Build Solution** (Release recommended).

Outputs (one DLL per project):

```text
GameData/zKerbalismSystemHeat/PluginData/zKerbalismSystemHeat.dll
GameData/zKerbalismFFT/PluginData/zKerbalismFFT.dll
GameData/zKerbalismNFE/PluginData/zKerbalismNFE.dll
GameData/zKerbalismDynamicRadiation/PluginData/zKerbalismDynamicRadiation.dll
```

KSP / mod references use the shared layout `../KSPDLL/` (sibling of this repo under `C#/`). Intermediate files stay under each project's `obj/`.

Command line:

```text
msbuild src\KerbalismSystemHeatSupport.sln /p:Configuration=Release
```

## Optional patches

| Path | Mod |
|------|-----|
| `Extras/zKerbalismSystemHeat/SystemHeatFissionReactorsLowerMinThrust/` | Lower fission reactor minimum throttle |
| `Extras/zKerbalismFFT/FFTFusionReactorsLowerMinThrust/` | Lower fusion reactor minimum throttle |

Copy the desired extra folder into KSP `GameData/`. Optional patches are also included inside each mod release zip under `Extras/` (see below).

## Release packages

After a **Release** build, create four player zips (GameData + Extras + LICENSE + README + CHANGELOG). **`-Version` is manual** — any release label works (`1.0.0`, `1.0.0-beta.1`, `v1.0.0-beta.1`; a leading `v` is optional).

```powershell
.\scripts\package-release.ps1 -Version 1.0.0-beta.1
```

Skip MSBuild if DLLs are already built:

```powershell
.\scripts\package-release.ps1 -Version 1.0.0-beta.1 -SkipBuild
```

Output file names (example for `1.0.0-beta.1`):

```text
dist/KerbalismSystemHeat.v1.0.0-beta.1.zip
dist/KerbalismFFT.v1.0.0-beta.1.zip
dist/KerbalismNFE.v1.0.0-beta.1.zip
dist/KerbalismDynamicRadiation.v1.0.0-beta.1.zip
```

Each zip is extracted into the KSP install root (merges `GameData/`). Contents:

| Path in zip | Contents |
|-------------|----------|
| `GameData/zKerbalismXXX/` | Mod cfg, host manifest, PluginData DLL |
| `Extras/zKerbalismXXX/` | Optional MM patches (SystemHeat & FFT only) |
| `LICENSE` | MIT license |
| `README.md` | Per-mod readme (from `docs/mods/`) |
| `CHANGELOG.md` | This mod's release notes (from root `CHANGELOG.md`) |

Upload all four files to one GitHub Release (e.g. tag `v1.0.0-beta.1`, matching `-Version`).

## Repository layout

```text
KerbalismSystemHeatSupport/
├── README.md
├── CHANGELOG.md
├── LICENSE
├── docs/mods/          detailed per-mod READMEs
├── docs/changelog/     upstream history (judicator forks)
├── docs/legal/         attribution
├── src/                four C# projects + solution
├── scripts/            release packaging (package-release.ps1)
├── dist/               generated zips (gitignored)
├── GameData/           four mod GameData trees
└── Extras/             optional MM patches
```

## Licensing

MIT License — see [LICENSE](LICENSE). Fork attribution: [docs/legal/ATTRIBUTION.md](docs/legal/ATTRIBUTION.md).

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
