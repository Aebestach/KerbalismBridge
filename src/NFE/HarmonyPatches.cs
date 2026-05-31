using HarmonyLib;
using NearFutureElectrical;

namespace KerbalismNFE
{
	internal static class KerbalismNFEHarmony
	{
		internal static void ApplyPatches()
		{
			try
			{
				var harmony = new Harmony("KerbalismNFE");
				harmony.PatchAll(typeof(KerbalismNFEHarmony).Assembly);
				KNFEUtils.Log("Harmony patches applied.");
			}
			catch (System.Exception ex)
			{
				KNFEUtils.LogError("Harmony patch setup failed: " + ex);
			}
		}
	}

	[HarmonyPatch(typeof(DischargeCapacitor), "OnFixedUpdate")]
	internal static class Patch_DischargeCapacitor_OnFixedUpdate
	{
		private static bool Prefix(DischargeCapacitor __instance)
		{
			return __instance.part.FindModuleImplementing<NFECapacitorKerbalismUpdater>() == null;
		}
	}

	[HarmonyPatch(typeof(DischargeCapacitor), "DoCatchup")]
	internal static class Patch_DischargeCapacitor_DoCatchup
	{
		private static bool Prefix(DischargeCapacitor __instance)
		{
			return __instance.part.FindModuleImplementing<NFECapacitorKerbalismUpdater>() == null;
		}
	}
}
