> Part of [Kerbalism Bridge](../../README.md). Build: `src/KerbalismBridge.sln`.
# zKerbalismResourceAudit

**Version:** 1.0.0

Optional Kerbalism Bridge satellite that runs a **static full-part-database scan** after load and writes English reports under the KSP `Logs` folder.

## What it reports

A finding is any part that still has a **stock or native resource module** (for example `ModuleResourceConverter`) while that module is **not** treated as integrated with Kerbalism or Kerbalism Bridge:

- Kerbalism modules such as `ProcessController`, `Harvester`, `Configure`, or `IKerbalismModule`
- Bridge `*KerbalismUpdater` sidecars (with optional native/updater pairs in `Settings.cfg`)
- Public `ResourceUpdate` hooks

No in-flight scanning is performed.

## Log output

After each scan (once per game session, at main menu):

```
{KSP}/Logs/zKerbalismResourceAudit/scan_YYYY-MM-DD_HHmmss/
  summary.log
  parts-unintegrated.log
  by-mod.log
  by-module.log
```

## Requirements

| Required | Optional |
|----------|----------|
| Kerbalism | Kerbalism Bridge (helps reduce findings when patches are installed) |
| [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) | Module Manager (same as Kerbalism) |

## Install

Copy `GameData/zKerbalismResourceAudit` into KSP `GameData` and install **zKerbalismPluginHost** separately.

```text
msbuild src\KerbalismBridge.sln /p:Configuration=Release /t:zKerbalismResourceAudit
```

Output:

- `GameData/zKerbalismResourceAudit/PluginData/zKerbalismResourceAudit.dll`
- `GameData/zKerbalismResourceAudit/zKerbalismResourceAudit.host.xml`

Do not place the DLL in `Plugins/`.

## Settings

Edit `GameData/zKerbalismResourceAudit/Settings.cfg`:

- `Enabled` — run scan at main menu
- `LogToUnity` — print log path in the KSP console
- `SuspiciousModule` / `IntegrationModule` / `NativeModuleUpdaterPair` — override defaults
- `IgnoreMod` / `IgnorePart` — suppress noise (for example stock `Squad`)

A reported part is not always a bug: mods that intentionally keep stock resource logic will appear until you add Bridge patches or ignore rules.
