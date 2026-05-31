# Upstream changelog: judicator/KerbalismFFT

Historical releases before the Aebestach fork (through 0.2.0).

## Upstream changelog (judicator/KerbalismFFT, through 0.2.0)

0.2.0
-----
 - Removed dynamic radioactivity feature completely (in order to fix issues with FAR and KopernicusExpansion, feature could be migrated to separate mod).
 * Hopefully fixed issue #1, thus making mod compatible with FAR and KopernicusExpansion. Need more testing though.

0.1.1
-----
 * Actually added Emitter partmodule for fusion reactors.
 + Added Settings.cfg. If you feel that FFT engines or fusion reactors are too radioactive, feel free to set emission coefficients for them in this file as you see fit.
 * Somewhat rebalanced emission values for FFT engines.
 + Added optional patch (Extras/FFTFusionReactorsLowerMinThrust): sets minimum throttle to fusion reactors to 5% (default is 10%). Could help you save some De/He3 on long journeys.
 + Added KerbalismSystemHeat 0.4.1 to download. No need to download it separately now.

0.1.0
-----
 - First release
