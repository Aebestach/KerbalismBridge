using HarmonyLib;
using KERBALISM;
using KerbalismBridge;

namespace KerbalismProcess
{
	// KSPCF FastLoader can populate GameDatabase before ModuleManager finishes patching.
	// Kerbalism parses profiles at Startup.Instantly, so support profiles patched in the
	// same session (e.g. zKerbalismSterlingSystems No-RR processes) are missed unless we
	// reload after MM's post-load callback when the final config cache is available.
	[HarmonyPatch(typeof(Loader), nameof(Loader.ModuleManagerPostLoad))]
	internal static class Patch_Loader_ModuleManagerPostLoad
	{
		private static void Postfix()
		{
			Profile.Parse();
			BridgeUtils.Log("Reloaded Kerbalism profiles after ModuleManager post-load.");
		}
	}
}
