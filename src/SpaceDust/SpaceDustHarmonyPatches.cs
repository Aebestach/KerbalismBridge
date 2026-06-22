using System;
using System.Reflection;
using HarmonyLib;
using KERBALISM;
using KerbalismBridge;
using SpaceDust;

namespace KerbalismSpaceDust
{
	internal static class SpaceDustHarmonyPatches
	{
		private static bool patchesApplied;

		internal static void ApplyPatches()
		{
			if (patchesApplied)
				return;

			MethodInfo fixedUpdate = typeof(ModuleSpaceDustHarvester).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fixedUpdate == null)
				return;

			var harmony = new Harmony("KerbalismSpaceDust");
			harmony.Patch(
				fixedUpdate,
				prefix: new HarmonyMethod(typeof(SpaceDustHarmonyPatches), nameof(SpaceDustFixedUpdatePrefix)),
				postfix: new HarmonyMethod(typeof(SpaceDustHarmonyPatches), nameof(SpaceDustFixedUpdatePostfix)));

			MethodInfo backgroundProcess = typeof(SpaceDustHarvesterBackground).GetMethod(
				nameof(SpaceDustHarvesterBackground.Process),
				BindingFlags.Instance | BindingFlags.Public);
			if (backgroundProcess != null)
			{
				harmony.Patch(
					backgroundProcess,
					prefix: new HarmonyMethod(typeof(SpaceDustHarmonyPatches), nameof(SpaceDustBackgroundProcessPrefix)));
			}

			patchesApplied = true;
			harmony.PatchAll(Assembly.GetExecutingAssembly());
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

		private static bool SpaceDustBackgroundProcessPrefix(ProtoPartModuleSnapshot ___protoMiner, Vessel ___ves)
		{
			ProtoPartModuleSnapshot harvester = ___protoMiner;
			Vessel vessel = ___ves;

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
