using HarmonyLib;
using KERBALISM;
using KerbalismBridge;

namespace KerbalismProcess
{
	public static class KerbalismProcessHarmony
	{
		private static bool patchesApplied;

		public static void ApplyPatches()
		{
			if (patchesApplied)
				return;
			patchesApplied = true;
			var harmony = new Harmony("KerbalismProcess");
			harmony.PatchAll(typeof(KerbalismProcessHarmony).Assembly);
			BridgeUtils.Log("Process layer Harmony patches applied.");
		}
	}

	[HarmonyPatch(typeof(ProcessController), "SetRunning")]
	internal static class Patch_ProcessController_SetRunning
	{
		private static void Postfix(ProcessController __instance)
		{
			if (__instance is ProcessControllerSystemHeat heatController)
				heatController.OnRunningChanged();
		}
	}

	[HarmonyPatch(typeof(Harvester), "Toggle")]
	internal static class Patch_Harvester_Toggle
	{
		private static void Postfix(Harvester __instance)
		{
			if (__instance is HarvesterSystemHeat && Lib.IsEditor())
				Lib.RefreshPlanner();
		}
	}
}
