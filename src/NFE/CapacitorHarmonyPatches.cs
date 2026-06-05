using HarmonyLib;
using KERBALISM;
using NearFutureElectrical;

namespace KerbalismNFE
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

	[HarmonyPatch(typeof(DischargeCapacitor), nameof(DischargeCapacitor.Enable))]
	internal static class Patch_DischargeCapacitor_Enable
	{
		private static void Postfix(DischargeCapacitor __instance)
		{
			if (__instance.part.FindModuleImplementing<NFECapacitorKerbalismUpdater>() != null)
				Lib.RefreshPlanner();
		}
	}

	[HarmonyPatch(typeof(DischargeCapacitor), nameof(DischargeCapacitor.Disable))]
	internal static class Patch_DischargeCapacitor_Disable
	{
		private static void Postfix(DischargeCapacitor __instance)
		{
			if (__instance.part.FindModuleImplementing<NFECapacitorKerbalismUpdater>() != null)
				Lib.RefreshPlanner();
		}
	}

	[HarmonyPatch(typeof(DischargeCapacitor), nameof(DischargeCapacitor.Discharge))]
	internal static class Patch_DischargeCapacitor_Discharge
	{
		private static void Postfix(DischargeCapacitor __instance)
		{
			if (__instance.part.FindModuleImplementing<NFECapacitorKerbalismUpdater>() != null)
				Lib.RefreshPlanner();
		}
	}

	[HarmonyPatch(typeof(DischargeCapacitor), "ToggleAction")]
	internal static class Patch_DischargeCapacitor_ToggleAction
	{
		private static void Postfix(DischargeCapacitor __instance)
		{
			if (__instance.part.FindModuleImplementing<NFECapacitorKerbalismUpdater>() != null)
				Lib.RefreshPlanner();
		}
	}
}
