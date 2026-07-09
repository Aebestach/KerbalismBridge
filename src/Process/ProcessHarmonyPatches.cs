using HarmonyLib;
using KERBALISM;
using KerbalismBridge;
using SystemHeat;

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
			if (!value || Lib.IsEditor())
				return;

			if (__instance is ProcessControllerSystemHeat heatController
				&& heatController.RequiresDeployGate()
				&& !heatController.IsDeployedForUse())
				value = false;

			if (__instance is ProcessControllerDeployable deployableController
				&& deployableController.RequiresDeployGate()
				&& !deployableController.IsDeployedForUse())
				value = false;
		}

		private static void Postfix(ProcessController __instance)
		{
			if (__instance is ProcessControllerSystemHeat heatController)
				heatController.OnRunningChanged();
			else if (__instance is ProcessControllerDeployable deployableController)
				deployableController.OnRunningChanged();
		}
	}

	internal static class ProcessDeploySync
	{
		internal static void FromAnimator(Part part, bool deployStarted = false)
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
				{
					if (deployStarted)
						module.MarkDeployStarted();
					else
						module.EnableModule();
				}
				else
					module.DisableModule();
			}

			foreach (ProcessControllerDeployable module in part.FindModulesImplementing<ProcessControllerDeployable>())
			{
				if (!module.RequiresDeployGate())
					continue;

				if (animator.isDeployed)
				{
					if (deployStarted)
						module.MarkDeployStarted();
					else
						module.EnableModule();
				}
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
			ProcessDeploySync.FromAnimator(__instance.part, deployStarted: true);
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

	// Stock SystemHeatVessel.FixedUpdate() (Simulator.Simulate()) is allowed to run normally in every
	// state, so SystemHeat keeps all its own loop bookkeeping (consumedSystemFlux, nominal temp,
	// convection, allocation). Simulate() applies no irreversible side effects (core damage / shutdown
	// come from the modules, e.g. ProcessControllerSystemHeat), so it is safe to let the known hyperwarp
	// stale-flux temperature spike happen inside the stock tick and correct it immediately after. The
	// postfix: (a) refreshes the last-good anchor on sane loaded+unpacked frames, and (b) hands the active
	// vessel to the bridge stabilizer, which self-gates to the loaded hyperwarp scope (fixedDt >= 10 s,
	// packed or the brief unpacked catch-up frame).
	[HarmonyPatch(typeof(SystemHeatVessel), "FixedUpdate")]
	internal static class Patch_SystemHeatVessel_FixedUpdate
	{
		private static void Postfix(SystemHeatVessel __instance)
		{
			Vessel v = Traverse.Create(__instance).Field("vessel").GetValue<Vessel>();
			if (v == null)
				return;

			// Rolling last-good anchor for the active loaded/unpacked vessel on a sane step (no-op while packed).
			SystemHeatBackgroundThermal.CaptureLoadedAnchorIfSane(v);

			// Correct the loaded hyperwarp spike after stock has run (self-gated to large fixedDeltaTime).
			SystemHeatBackgroundThermal.StabilizeLoadedHyperwarpTransition(v, TimeWarp.fixedDeltaTime);
		}
	}
}
