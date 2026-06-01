using HarmonyLib;
using NearFutureElectrical;

namespace KerbalismNative
{
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
