using HarmonyLib;
using KerbalismBridge;

namespace KerbalismNFE
{
	public static class KerbalismNFEHarmony
	{
		private static bool patchesApplied;

		public static void ApplyPatches()
		{
			if (patchesApplied)
				return;
			patchesApplied = true;
			var harmony = new Harmony("KerbalismNFE");
			harmony.PatchAll(typeof(KerbalismNFEHarmony).Assembly);
			BridgeUtils.Log("NFE satellite Harmony patches applied.");
		}
	}
}
