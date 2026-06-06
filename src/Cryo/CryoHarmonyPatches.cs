using System;
using System.Reflection;
using HarmonyLib;
using KERBALISM;

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
				var harmony = new Harmony("KerbalismCryo");
				PatchProcessCryoTank(harmony);
				// ModuleCryoTank only: block stock EC drain (#717). SH cryo uses loop heat, keep native FixedUpdate when loaded.
				PatchNativeFixedUpdate(harmony, "ModuleCryoTank");

				patchesApplied = true;
				CryoUtils.Log("Harmony patches applied.");
			}
			catch (Exception ex)
			{
				CryoUtils.LogError("Harmony setup failed: " + ex);
			}
		}

		static void PatchProcessCryoTank(Harmony harmony)
		{
			MethodInfo target = typeof(Background).GetMethod("ProcessCryoTank", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo prefix = typeof(Patch_Kerbalism_ProcessCryoTank).GetMethod(nameof(Patch_Kerbalism_ProcessCryoTank.Prefix), BindingFlags.Static | BindingFlags.Public);
			if (target == null || prefix == null)
			{
				CryoUtils.LogError("Could not patch Background.ProcessCryoTank.");
				return;
			}

			harmony.Patch(target, prefix: new HarmonyMethod(prefix));
		}

		static void PatchNativeFixedUpdate(Harmony harmony, string moduleName)
		{
			Type targetType = FindPartModuleType(moduleName);
			if (targetType == null)
			{
				CryoUtils.Log("PartModule type not loaded: " + moduleName);
				return;
			}

			MethodInfo target = targetType.GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo prefix = typeof(Patch_CryoNative_FixedUpdate).GetMethod(nameof(Patch_CryoNative_FixedUpdate.Prefix), BindingFlags.Static | BindingFlags.Public);
			if (target == null || prefix == null)
			{
				CryoUtils.LogError("Could not patch " + moduleName + ".FixedUpdate.");
				return;
			}

			harmony.Patch(target, prefix: new HarmonyMethod(prefix));
		}

		static Type FindPartModuleType(string moduleName)
		{
			if (moduleName == "ModuleSystemHeatCryoTank")
				return CryoUtils.ResolveSystemHeatCryoTankType();

			foreach (AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies)
			{
				Type type = loaded.assembly.GetType("CryoTanks." + moduleName, false)
					?? loaded.assembly.GetType(moduleName, false);
				if (type != null && typeof(PartModule).IsAssignableFrom(type))
					return type;
			}

			return null;
		}

	}

	internal static class Patch_Kerbalism_ProcessCryoTank
	{
		public static bool Prefix(ProtoPartSnapshot p)
		{
			return !CryoUtils.PartHasCryoUpdater(p);
		}
	}

	internal static class Patch_CryoNative_FixedUpdate
	{
		public static bool Prefix(PartModule __instance)
		{
			if (__instance == null || __instance.part == null)
				return true;

			if (__instance.moduleName == "ModuleSystemHeatCryoTank")
				return true;

			return __instance.part.FindModuleImplementing<CryoTankKerbalismUpdater>() == null;
		}
	}
}
