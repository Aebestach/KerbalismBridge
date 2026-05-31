using System;
using System.Linq;
using System.Reflection;
using FarFutureTechnologies;
using KERBALISM;
using UnityEngine;

namespace KerbalismFFT
{
	internal static class KerbalismFFTHarmony
	{
		private const string HarmonyAssemblyName = "0Harmony";
		private static bool patchesApplied;

		internal static void ApplyPatches()
		{
			if (patchesApplied)
				return;

			Assembly harmonyAssembly = FindLoadedAssembly(HarmonyAssemblyName);
			if (harmonyAssembly == null)
				return;

			try
			{
				Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony");
				Type harmonyMethodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod");
				if (harmonyType == null || harmonyMethodType == null)
				{
					KFFTUtils.LogError("HarmonyLib types not found in 0Harmony assembly.");
					return;
				}

				object harmony = Activator.CreateInstance(harmonyType, "KerbalismFFT");
				MethodInfo patchMethod = harmonyType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
					.FirstOrDefault(m => m.Name == "Patch" && m.GetParameters().Length >= 2);
				if (patchMethod == null)
				{
					KFFTUtils.LogError("Harmony.Patch method not found.");
					return;
				}

				PatchPrefix(harmony, patchMethod, harmonyMethodType, typeof(FusionReactor), "GeneratePower", typeof(Patch_FusionReactor_GeneratePower), nameof(Patch_FusionReactor_GeneratePower.Prefix));
				PatchPrefix(harmony, patchMethod, harmonyMethodType, typeof(FusionReactor), "RechargeCapacitors", typeof(Patch_FusionReactor_RechargeCapacitors), nameof(Patch_FusionReactor_RechargeCapacitors.Prefix));
				PatchPostfix(harmony, patchMethod, harmonyMethodType, typeof(Computer), "GetModuleDevices", typeof(Patch_Computer_GetModuleDevices), nameof(Patch_Computer_GetModuleDevices.Postfix));

				patchesApplied = true;
				KFFTUtils.Log("Harmony patches applied.");
			}
			catch (Exception ex)
			{
				KFFTUtils.LogError("Harmony patch setup failed: " + ex);
			}
		}

		private static void PatchPrefix(object harmony, MethodInfo patchMethod, Type harmonyMethodType, Type targetType, string targetMethodName, Type patchType, string prefixMethodName)
		{
			MethodInfo target = targetType.GetMethod(targetMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo prefix = patchType.GetMethod(prefixMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (target == null || prefix == null)
			{
				KFFTUtils.LogError("Could not find patch target: " + targetType.Name + "." + targetMethodName);
				return;
			}

			object prefixHarmonyMethod = Activator.CreateInstance(harmonyMethodType, prefix);
			InvokePatch(harmony, patchMethod, target, prefixHarmonyMethod, null);
		}

		private static void PatchPostfix(object harmony, MethodInfo patchMethod, Type harmonyMethodType, Type targetType, string targetMethodName, Type patchType, string postfixMethodName)
		{
			MethodInfo target = targetType.GetMethod(targetMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo postfix = patchType.GetMethod(postfixMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (target == null || postfix == null)
			{
				KFFTUtils.LogError("Could not find patch target: " + targetType.Name + "." + targetMethodName);
				return;
			}

			object postfixHarmonyMethod = Activator.CreateInstance(harmonyMethodType, postfix);
			InvokePatch(harmony, patchMethod, target, null, postfixHarmonyMethod);
		}

		private static void InvokePatch(object harmony, MethodInfo patchMethod, MethodInfo target, object prefixHarmonyMethod, object postfixHarmonyMethod)
		{
			ParameterInfo[] parameters = patchMethod.GetParameters();
			if (parameters.Length == 5)
			{
				patchMethod.Invoke(harmony, new[] { target, prefixHarmonyMethod, postfixHarmonyMethod, null, null });
				return;
			}

			if (parameters.Length == 4)
			{
				patchMethod.Invoke(harmony, new[] { target, prefixHarmonyMethod, postfixHarmonyMethod, null });
				return;
			}

			patchMethod.Invoke(harmony, new[] { target, prefixHarmonyMethod ?? postfixHarmonyMethod });
		}

		private static void InvokePatch(object harmony, MethodInfo patchMethod, MethodInfo target, object prefixHarmonyMethod)
		{
			InvokePatch(harmony, patchMethod, target, prefixHarmonyMethod, null);
		}

		private static Assembly FindLoadedAssembly(string assemblyName)
		{
			foreach (AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies)
			{
				if (loaded.assembly.GetName().Name == assemblyName)
					return loaded.assembly;
			}
			return null;
		}
	}

	internal static class Patch_FusionReactor_GeneratePower
	{
		internal static bool Prefix(FusionReactor __instance)
		{
			return __instance.part.FindModuleImplementing<FFTFusionReactorKerbalismUpdater>() == null
				&& __instance.part.FindModuleImplementing<FFTFusionEngineKerbalismUpdater>() == null;
		}
	}

	internal static class Patch_FusionReactor_RechargeCapacitors
	{
		internal static bool Prefix(FusionReactor __instance)
		{
			return __instance.part.FindModuleImplementing<FFTFusionReactorKerbalismUpdater>() == null
				&& __instance.part.FindModuleImplementing<FFTFusionEngineKerbalismUpdater>() == null;
		}
	}
}
