using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using KERBALISM;
using SystemHeat;
using KerbalismBridge;

namespace KerbalismNative
{
	/// <summary>
	/// Routes ModuleSystemHeatConverter resource IO through Kerbalism while keeping native SystemHeat behaviour.
	/// </summary>
	public class SystemHeatConverterKerbalismUpdater : PartModule, IKerbalismModule
	{
		public static string brokerName = "SHNativeConverter";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismBridge_Brokers_Converter");

		[KSPField(isPersistant = true)]
		public string converterModuleID = "converter";

		protected ModuleSystemHeatConverter converterModule;

		internal bool OwnsConverter(ModuleSystemHeatConverter converter)
		{
			return converter != null
				&& converterModuleID == converter.moduleID
				&& converter.part == part;
		}

		internal ModuleSystemHeatConverter FindConverterModule()
		{
			if (converterModule != null)
				return converterModule;

			converterModule = part.GetComponents<ModuleSystemHeatConverter>()
				.FirstOrDefault(x => x.moduleID == converterModuleID);

			if (converterModule == null)
				converterModule = part.GetComponents<ModuleSystemHeatConverter>().FirstOrDefault();

			return converterModule;
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			return SHNativeConverterResourceSim.AddLoadedConverterRates(
				FindConverterModule(),
				brokerTitle,
				resourceChangeRequest);
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			ModuleSystemHeatConverter converter = FindConverterModule();
			if (converter != null && converter.IsActivated)
				return SHNativeConverterResourceSim.AddPlannerConverterRates(converter, resourceChangeRequest, brokerTitle);
			return brokerTitle;
		}

		public static string BackgroundUpdate(
			Vessel v,
			ProtoPartSnapshot part_snapshot,
			ProtoPartModuleSnapshot module_snapshot,
			PartModule proto_part_module,
			Part proto_part,
			Dictionary<string, double> availableResources,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			double elapsed_s)
		{
			var updater = proto_part_module as SystemHeatConverterKerbalismUpdater;
			string moduleId = Lib.Proto.GetString(module_snapshot, "converterModuleID");
			ProtoPartModuleSnapshot converterSnapshot = null;
			ModuleSystemHeatConverter converterPrefab = null;

			foreach (ProtoPartModuleSnapshot module in part_snapshot.modules)
			{
				if (module.moduleName != "ModuleSystemHeatConverter")
					continue;

				ModuleSystemHeatConverter prefab = proto_part.FindModuleImplementing<ModuleSystemHeatConverter>();
				if (prefab != null && prefab.moduleID == moduleId)
				{
					converterSnapshot = module;
					converterPrefab = prefab;
					break;
				}
			}

			if (converterSnapshot == null)
			{
				converterSnapshot = BridgeUtils.TryFindPartModuleSnapshot(part_snapshot, "ModuleSystemHeatConverter");
				converterPrefab = proto_part.FindModuleImplementing<ModuleSystemHeatConverter>();
			}

			if (converterSnapshot != null && converterPrefab != null)
			{
				SHNativeConverterResourceSim.BackgroundUpdateConverter(
					v,
					converterSnapshot,
					converterPrefab,
					brokerName,
					brokerTitle,
					elapsed_s);
			}

			SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
			return brokerTitle;
		}
	}
}
