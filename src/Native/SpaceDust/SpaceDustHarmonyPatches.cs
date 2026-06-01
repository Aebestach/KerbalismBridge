using System;
using System.Reflection;
using HarmonyLib;
using KerbalismBridge;

namespace KerbalismNative
{
	internal static class SpaceDustHarmonyPatches
	{
		internal static void ApplyOptionalPatches(Harmony harmony)
		{
			Type spaceDustHarvester = AccessTools.TypeByName("SpaceDust.ModuleSpaceDustHarvester");
			if (spaceDustHarvester == null)
			{
				BridgeUtils.Log("SpaceDust not loaded; skipping ModuleSpaceDustHarvester patches.");
				return;
			}

			MethodInfo fixedUpdate = AccessTools.Method(spaceDustHarvester, "FixedUpdate");
			if (fixedUpdate == null)
				return;

			harmony.Patch(
				fixedUpdate,
				prefix: new HarmonyMethod(typeof(SpaceDustHarmonyPatches), nameof(SpaceDustFixedUpdatePrefix)),
				postfix: new HarmonyMethod(typeof(SpaceDustHarmonyPatches), nameof(SpaceDustFixedUpdatePostfix)));
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
