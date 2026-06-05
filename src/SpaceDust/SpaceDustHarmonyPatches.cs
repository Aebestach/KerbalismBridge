using System;
using System.Reflection;
using HarmonyLib;
using KerbalismBridge;

namespace KerbalismSpaceDust
{
	internal static class SpaceDustHarmonyPatches
	{
		private static bool patchesApplied;

		internal static void ApplyPatches()
		{
			if (patchesApplied)
				return;

			Type spaceDustHarvester = AccessTools.TypeByName("SpaceDust.ModuleSpaceDustHarvester");
			if (spaceDustHarvester == null)
			{
				BridgeUtils.Log("SpaceDust not loaded; skipping ModuleSpaceDustHarvester patches.");
				return;
			}

			MethodInfo fixedUpdate = AccessTools.Method(spaceDustHarvester, "FixedUpdate");
			if (fixedUpdate == null)
				return;

			var harmony = new Harmony("KerbalismSpaceDust");
			harmony.Patch(
				fixedUpdate,
				prefix: new HarmonyMethod(typeof(SpaceDustHarmonyPatches), nameof(SpaceDustFixedUpdatePrefix)),
				postfix: new HarmonyMethod(typeof(SpaceDustHarmonyPatches), nameof(SpaceDustFixedUpdatePostfix)));

			patchesApplied = true;
			BridgeUtils.Log("SpaceDust satellite Harmony patches applied.");
		}

		private static void SpaceDustFixedUpdatePrefix(PartModule __instance)
		{
			if (__instance.part.FindModuleImplementing<SpaceDustHarvesterKerbalismUpdater>() != null)
				SpaceDustResourceBlocker.EnterBlock();
		}

		private static void SpaceDustFixedUpdatePostfix(PartModule __instance)
		{
			if (__instance.part.FindModuleImplementing<SpaceDustHarvesterKerbalismUpdater>() != null)
				SpaceDustResourceBlocker.ExitBlock();
		}
	}
}
