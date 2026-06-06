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
		private static void Prefix(ProcessController __instance, ref bool value)
		{
			if (value
				&& __instance is ProcessControllerSystemHeat heatController
				&& heatController.RequiresDeployGate()
				&& !heatController.IsDeployedForUse()
				&& !Lib.IsEditor())
				value = false;
		}

		private static void Postfix(ProcessController __instance)
		{
			if (__instance is ProcessControllerSystemHeat heatController)
				heatController.OnRunningChanged();
		}
	}

	internal static class ProcessDeploySync
	{
		internal static void FromAnimator(Part part)
		{
			if (part == null)
				return;

			ModuleAnimationGroup animator = part.FindModuleImplementing<ModuleAnimationGroup>();
			if (animator == null)
				return;

			foreach (ProcessControllerSystemHeat module in part.FindModulesImplementing<ProcessControllerSystemHeat>())
			{
				if (!module.RequiresDeployGate())
					continue;

				if (animator.isDeployed)
					module.EnableModule();
				else
					module.DisableModule();
			}
		}
	}

	[HarmonyPatch(typeof(ModuleAnimationGroup), "DeployModule")]
	internal static class Patch_ModuleAnimationGroup_DeployModule
	{
		private static void Postfix(ModuleAnimationGroup __instance)
		{
			ProcessDeploySync.FromAnimator(__instance.part);
		}
	}

	[HarmonyPatch(typeof(ModuleAnimationGroup), "RetractModule")]
	internal static class Patch_ModuleAnimationGroup_RetractModule
	{
		private static void Postfix(ModuleAnimationGroup __instance)
		{
			ProcessDeploySync.FromAnimator(__instance.part);
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
