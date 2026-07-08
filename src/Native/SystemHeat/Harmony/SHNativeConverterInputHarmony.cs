using System.Collections.Generic;
using HarmonyLib;
using KERBALISM;
using SystemHeat;

namespace KerbalismNative
{
	/// <summary>
	/// While Kerbalism owns SH native converter/harvester resource IO, hide stock
	/// resource rates during native FixedUpdateFlight so native side effects still run.
	/// </summary>
	internal static class SHNativeConverterInputHarmony
	{
		internal struct ResourceListRateBackup
		{
			internal double[] Ratios;
			internal bool Active;

			internal bool HasBackup => Active && Ratios != null;
		}

		internal struct ConverterRateBackup
		{
			internal ResourceListRateBackup Inputs;
			internal ResourceListRateBackup Outputs;

			internal bool HasBackup => Inputs.HasBackup || Outputs.HasBackup;
		}

		internal static bool ShouldZeroStockResourceRates(PartModule module)
		{
			if (module == null || module.part == null || !Lib.IsFlight())
				return false;

			if (module is ModuleSystemHeatConverter shConverter)
			{
				var updaters = module.part.FindModulesImplementing<SystemHeatConverterKerbalismUpdater>();
				for (int i = 0; i < updaters.Count; i++)
				{
					if (updaters[i].OwnsConverter(shConverter))
						return true;
				}
			}

			if (module is ModuleSystemHeatHarvester shHarvester)
			{
				var updaters = module.part.FindModulesImplementing<SystemHeatHarvesterKerbalismUpdater>();
				for (int i = 0; i < updaters.Count; i++)
				{
					if (updaters[i].OwnsHarvester(shHarvester))
						return true;
				}
			}

			return false;
		}

		internal static ResourceListRateBackup ZeroResourceList(List<ResourceRatio> resourceList)
		{
			if (resourceList == null || resourceList.Count == 0)
				return default;

			var backup = new ResourceListRateBackup
			{
				Ratios = new double[resourceList.Count],
				Active = true
			};

			for (int i = 0; i < resourceList.Count; i++)
			{
				ResourceRatio entry = resourceList[i];
				backup.Ratios[i] = entry.Ratio;
				entry.Ratio = 0.0;
				resourceList[i] = entry;
			}

			return backup;
		}

		internal static ConverterRateBackup ZeroConverterLists(ModuleSystemHeatConverter converter)
		{
			return new ConverterRateBackup
			{
				Inputs = ZeroResourceList(converter.inputList),
				Outputs = ZeroResourceList(converter.outputList)
			};
		}

		internal static void RestoreResourceList(List<ResourceRatio> resourceList, ref ResourceListRateBackup backup)
		{
			if (!backup.HasBackup || resourceList == null)
				return;

			int count = System.Math.Min(resourceList.Count, backup.Ratios.Length);
			for (int i = 0; i < count; i++)
			{
				ResourceRatio entry = resourceList[i];
				entry.Ratio = backup.Ratios[i];
				resourceList[i] = entry;
			}

			backup = default;
		}

		internal static void RestoreConverterLists(ModuleSystemHeatConverter converter, ref ConverterRateBackup backup)
		{
			RestoreResourceList(converter.inputList, ref backup.Inputs);
			RestoreResourceList(converter.outputList, ref backup.Outputs);
			backup = default;
		}
	}

	[HarmonyPatch(typeof(ModuleSystemHeatConverter), "FixedUpdateFlight")]
	internal static class Patch_SystemHeatConverter_FixedUpdateFlight
	{
		private static void Prefix(ModuleSystemHeatConverter __instance, ref SHNativeConverterInputHarmony.ConverterRateBackup __state)
		{
			if (!SHNativeConverterInputHarmony.ShouldZeroStockResourceRates(__instance))
				return;

			__state = SHNativeConverterInputHarmony.ZeroConverterLists(__instance);
		}

		private static void Postfix(ModuleSystemHeatConverter __instance, ref SHNativeConverterInputHarmony.ConverterRateBackup __state)
		{
			if (!__state.HasBackup)
				return;

			SHNativeConverterInputHarmony.RestoreConverterLists(__instance, ref __state);
		}
	}

	[HarmonyPatch(typeof(ModuleSystemHeatHarvester), "FixedUpdateFlight")]
	internal static class Patch_SystemHeatHarvester_FixedUpdateFlight
	{
		private static void Prefix(ModuleSystemHeatHarvester __instance, ref SHNativeConverterInputHarmony.ResourceListRateBackup __state)
		{
			if (!SHNativeConverterInputHarmony.ShouldZeroStockResourceRates(__instance))
				return;

			__state = SHNativeConverterInputHarmony.ZeroResourceList(__instance.inputList);
		}

		private static void Postfix(ModuleSystemHeatHarvester __instance, ref SHNativeConverterInputHarmony.ResourceListRateBackup __state)
		{
			if (!__state.HasBackup)
				return;

			SHNativeConverterInputHarmony.RestoreResourceList(__instance.inputList, ref __state);
		}
	}
}
