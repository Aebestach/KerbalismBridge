> Part of [KerbalismSystemHeatSupport](../../README.md). Build: `src/KerbalismSystemHeatSupport.sln`.
# Kerbalism Near Future Electrical

Middleman mod that routes Near Future Electrical **DischargeCapacitor** EC charge/discharge through Kerbalism, similar to the former DynamicBatteryStorage behavior in Planner and during background simulation.

## Supported parts

All parts with a `DischargeCapacitor` module (NFE capacitors).

## Behavior

- **Planner (VAB/SPH and flight):** shows capacitor charging (`-ChargeRate`) or discharging (`+dischargeActual`) EC/s when recharge/discharge is enabled.
- **Loaded vessels:** EC flows through Kerbalism brokers; `StoredCharge` is updated on the part.
- **Unloaded vessels:** Kerbalism background simulation updates EC and `StoredCharge`.
- **Automation (Planner â†?Automation â†?vessel devices):** each integrated capacitor appears as **NFE Capacitor Recharge** and **NFE Capacitor Discharge**, scriptable on loaded and unloaded vessels (requires Kerbalism `Automation = true`).
- **Native NFE EC IO** is blocked via Harmony when this updater is present (`OnFixedUpdate`, `DoCatchup`).

Capacitor PAW toggles (**Enable Recharge**, **Disable Recharge**, **Discharge Capacitor**, discharge rate slider) remain on the stock NFE module.

## Dependencies

* [Kerbalism](https://github.com/Kerbalism/Kerbalism)
* [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) â€?deferred loader for Kerbalism kbin releases
* [Near Future Electrical](https://github.com/post-kerbin-mining-corporation/NearFutureElectrical)
* [Module Manager](https://github.com/sarbian/ModuleManager)

## Installation

Copy `GameData/zKerbalismNFE` into your KSP `GameData` folder and install **zKerbalismPluginHost** separately. Building the solution outputs:

* `GameData/zKerbalismNFE/PluginData/zKerbalismNFE.dll` â€?plugin logic (loaded by zKerbalismPluginHost after Kerbalism is present)
* `GameData/zKerbalismNFE/zKerbalismNFE.host.xml` â€?host manifest (do not remove)

Do not place `zKerbalismNFE.dll` in `Plugins`, or KSP will load it before Kerbalism Bootstrap and fail.

## Notes

* DynamicBatteryStorage disables itself when Kerbalism is installed; this mod replaces that integration path for NFE capacitors only.
* Does not integrate NFE fission reactors (`nfe-reactor-*` Kerbalism processes or SystemHeat fission reactors) â€?use Kerbalism profile or KerbalismSystemHeat respectively.
