using System;
using HarmonyLib;
using KERBALISM;
using SimpleBoiloff;

namespace KerbalismCryo
{
	internal static class CryoHarmonyPatches
	{
		private static bool patchesApplied;

		internal static void ApplyPatches()
		{
			if (patchesApplied)
				return;

			try
			{
				new Harmony("KerbalismCryo").PatchAll(typeof(CryoHarmonyPatches).Assembly);
				patchesApplied = true;
				CryoUtils.Log("Harmony patches applied.");
			}
			catch (Exception ex)
			{
				CryoUtils.LogError("Harmony setup failed: " + ex);
			}
		}
	}

	[HarmonyPatch(typeof(Background), "ProcessCryoTank")]
	internal static class Patch_Kerbalism_ProcessCryoTank
	{
		public static bool Prefix(ProtoPartSnapshot p)
		{
			return !CryoUtils.PartHasCryoUpdater(p);
		}
	}

	[HarmonyPatch(typeof(ModuleCryoTank), "FixedUpdate")]
	internal static class Patch_ModuleCryoTank_FixedUpdate
	{
		public static bool Prefix(ModuleCryoTank __instance)
		{
			if (__instance == null || __instance.part == null)
				return true;

			return __instance.part.FindModuleImplementing<CryoTankKerbalismUpdater>() == null;
		}
	}
}
