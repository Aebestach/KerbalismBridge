using System;
using System.Linq;
using System.Reflection;
using KERBALISM;

namespace KerbalismCryo
{
	internal static class CryoHarmonyPatches
	{
		private const string HarmonyAssemblyName = "0Harmony";
		private static bool patchesApplied;

		internal static void ApplyPatches()
		{
			if (patchesApplied)
				return;

			Assembly harmonyAssembly = FindLoadedAssembly(HarmonyAssemblyName);
			if (harmonyAssembly == null)
			{
				CryoUtils.LogError("0Harmony not found; CryoTanks EC patches not applied.");
				return;
			}

			try
			{
				Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony");
				Type harmonyMethodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod");
				if (harmonyType == null || harmonyMethodType == null)
				{
					CryoUtils.LogError("HarmonyLib types not found.");
					return;
				}

				object harmony = Activator.CreateInstance(harmonyType, "KerbalismCryo");
				MethodInfo patchMethod = harmonyType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
					.FirstOrDefault(m => m.Name == "Patch" && m.GetParameters().Length >= 2);
				if (patchMethod == null)
				{
					CryoUtils.LogError("Harmony.Patch not found.");
					return;
				}

				PatchProcessCryoTank(harmony, patchMethod, harmonyMethodType);
				// ModuleCryoTank only: block stock EC drain (#717). SH cryo uses loop heat, keep native FixedUpdate when loaded.
				PatchNativeFixedUpdate(harmony, patchMethod, harmonyMethodType, "ModuleCryoTank");

				patchesApplied = true;
				CryoUtils.Log("Harmony patches applied.");
			}
			catch (Exception ex)
			{
				CryoUtils.LogError("Harmony setup failed: " + ex);
			}
		}

		static void PatchProcessCryoTank(object harmony, MethodInfo patchMethod, Type harmonyMethodType)
		{
			MethodInfo target = typeof(Background).GetMethod("ProcessCryoTank", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo prefix = typeof(Patch_Kerbalism_ProcessCryoTank).GetMethod(nameof(Patch_Kerbalism_ProcessCryoTank.Prefix), BindingFlags.Static | BindingFlags.Public);
			if (target == null || prefix == null)
			{
				CryoUtils.LogError("Could not patch Background.ProcessCryoTank.");
				return;
			}

			object prefixHarmonyMethod = Activator.CreateInstance(harmonyMethodType, prefix);
			InvokePatch(harmony, patchMethod, target, prefixHarmonyMethod, null);
		}

		static void PatchNativeFixedUpdate(object harmony, MethodInfo patchMethod, Type harmonyMethodType, string moduleName)
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

			object prefixHarmonyMethod = Activator.CreateInstance(harmonyMethodType, prefix);
			InvokePatch(harmony, patchMethod, target, prefixHarmonyMethod, null);
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

		static void InvokePatch(object harmony, MethodInfo patchMethod, MethodInfo target, object prefix, object postfix)
		{
			ParameterInfo[] parameters = patchMethod.GetParameters();
			if (parameters.Length == 5)
				patchMethod.Invoke(harmony, new[] { target, prefix, postfix, null, null });
			else
				patchMethod.Invoke(harmony, new[] { target, prefix, postfix });
		}

		static Assembly FindLoadedAssembly(string assemblyName)
		{
			foreach (AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies)
			{
				if (loaded.assembly.GetName().Name == assemblyName)
					return loaded.assembly;
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
