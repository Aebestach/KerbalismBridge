using System;
using System.Reflection;
using HarmonyLib;
using KERBALISM;
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

			Type backgroundHarvester = AccessTools.TypeByName("SpaceDust.SpaceDustHarvesterBackground");
			MethodInfo backgroundProcess = backgroundHarvester != null
				? AccessTools.Method(backgroundHarvester, "Process")
				: null;
			if (backgroundProcess != null)
			{
				harmony.Patch(
					backgroundProcess,
					prefix: new HarmonyMethod(typeof(SpaceDustHarmonyPatches), nameof(SpaceDustBackgroundProcessPrefix)));
			}

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

		private static bool SpaceDustBackgroundProcessPrefix(object __instance)
		{
			ProtoPartModuleSnapshot harvester = AccessTools.Field(__instance.GetType(), "protoMiner")
				?.GetValue(__instance) as ProtoPartModuleSnapshot;
			Vessel vessel = AccessTools.Field(__instance.GetType(), "ves")
				?.GetValue(__instance) as Vessel;

			if (harvester == null || vessel?.protoVessel == null)
				return true;

			foreach (ProtoPartSnapshot part in vessel.protoVessel.protoPartSnapshots)
			{
				if (!part.modules.Contains(harvester))
					continue;

				bool hasKerbalismUpdater = part.modules.Exists(module => module.moduleName == "SpaceDustHarvesterKerbalismUpdater");
				if (!hasKerbalismUpdater)
					return true;

				Lib.Proto.Set(harvester, "Enabled", false);
				return false;
			}

			return true;
		}
	}
}
