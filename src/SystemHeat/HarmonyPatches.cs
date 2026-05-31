using HarmonyLib;
using KERBALISM;
using SystemHeat;

namespace KerbalismSystemHeat
{
	public static class KerbalismSystemHeatHarmony
	{
		private static bool patchesApplied;

		public static void ApplyPatches()
		{
			if (patchesApplied)
				return;
			patchesApplied = true;
			var harmony = new Harmony("KerbalismSystemHeat");
			harmony.PatchAll(typeof(KerbalismSystemHeatHarmony).Assembly);
			KSHUtils.Log("Harmony patches applied.");
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

	[HarmonyPatch(typeof(ModuleSystemHeatFissionReactor), "HandleResourceActivities")]
	internal static class Patch_FissionReactor_HandleResourceActivities
	{
		private static bool Prefix(ModuleSystemHeatFissionReactor __instance)
		{
			return __instance.part.FindModuleImplementing<SystemHeatFissionReactorKerbalismUpdater>() == null
				&& __instance.part.FindModuleImplementing<SystemHeatFissionEngineKerbalismUpdater>() == null;
		}
	}

	[HarmonyPatch(typeof(ModuleSystemHeatFissionReactor), "DoCatchup")]
	internal static class Patch_FissionReactor_DoCatchup
	{
		private static bool Prefix(ModuleSystemHeatFissionReactor __instance)
		{
			return __instance.part.FindModuleImplementing<SystemHeatFissionReactorKerbalismUpdater>() == null
				&& __instance.part.FindModuleImplementing<SystemHeatFissionEngineKerbalismUpdater>() == null;
		}
	}
}
