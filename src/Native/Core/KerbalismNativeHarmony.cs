using HarmonyLib;
using KerbalismBridge;

namespace KerbalismNative
{
	public static class KerbalismNativeHarmony
	{
		private static bool patchesApplied;

		public static void ApplyPatches()
		{
			if (patchesApplied)
				return;
			patchesApplied = true;
			var harmony = new Harmony("KerbalismNative");
			harmony.PatchAll(typeof(KerbalismNativeHarmony).Assembly);
			BridgeUtils.Log("Native core Layer B Harmony patches applied.");
		}
	}
}
