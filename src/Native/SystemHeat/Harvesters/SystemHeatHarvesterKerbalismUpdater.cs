using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using KERBALISM;
using SystemHeat;
using KerbalismBridge;

namespace KerbalismNative
{
	/// <summary>
	/// Routes ModuleSystemHeatHarvester resource IO through Kerbalism while keeping native SystemHeat behaviour.
	/// </summary>
	public class SystemHeatHarvesterKerbalismUpdater : PartModule, IKerbalismModule
	{
		public static string brokerName = "SHNativeHarvester";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismBridge_Brokers_Harvester");

		[KSPField(isPersistant = true)]
		public string harvesterModuleID = "harvester";

		protected ModuleSystemHeatHarvester harvesterModule;

		internal bool OwnsHarvester(ModuleSystemHeatHarvester harvester)
		{
			return harvester != null
				&& harvesterModuleID == harvester.moduleID
				&& harvester.part == part;
		}

		internal ModuleSystemHeatHarvester FindHarvesterModule()
		{
			if (harvesterModule != null)
				return harvesterModule;

			harvesterModule = part.GetComponents<ModuleSystemHeatHarvester>()
				.FirstOrDefault(x => x.moduleID == harvesterModuleID);

			if (harvesterModule == null)
				harvesterModule = part.GetComponents<ModuleSystemHeatHarvester>().FirstOrDefault();

			return harvesterModule;
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			return SHNativeConverterResourceSim.AddLoadedHarvesterRates(
				FindHarvesterModule(),
				brokerTitle,
				resourceChangeRequest);
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			ModuleSystemHeatHarvester harvester = FindHarvesterModule();
			if (harvester != null && harvester.IsActivated)
				return SHNativeConverterResourceSim.AddPlannerHarvesterRates(harvester, resourceChangeRequest, brokerTitle);
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
			string moduleId = Lib.Proto.GetString(module_snapshot, "harvesterModuleID");
			ProtoPartModuleSnapshot harvesterSnapshot = null;
			ModuleSystemHeatHarvester harvesterPrefab = null;

			foreach (ProtoPartModuleSnapshot module in part_snapshot.modules)
			{
				if (module.moduleName != "ModuleSystemHeatHarvester")
					continue;

				ModuleSystemHeatHarvester prefab = proto_part.FindModuleImplementing<ModuleSystemHeatHarvester>();
				if (prefab != null && prefab.moduleID == moduleId)
				{
					harvesterSnapshot = module;
					harvesterPrefab = prefab;
					break;
				}
			}

			if (harvesterSnapshot == null)
			{
				harvesterSnapshot = BridgeUtils.TryFindPartModuleSnapshot(part_snapshot, "ModuleSystemHeatHarvester");
				harvesterPrefab = proto_part.FindModuleImplementing<ModuleSystemHeatHarvester>();
			}

			if (harvesterSnapshot != null && harvesterPrefab != null)
			{
				SHNativeConverterResourceSim.BackgroundUpdateHarvester(
					v,
					harvesterSnapshot,
					harvesterPrefab,
					brokerName,
					brokerTitle,
					elapsed_s);
			}

			SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
			return brokerTitle;
		}
	}
}
