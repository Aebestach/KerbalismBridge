using System.Collections.Generic;
using KERBALISM;
using SystemHeat;

namespace KerbalismCryo
{
	public class SystemHeatCryoTankKerbalismUpdater : PartModule, IKerbalismModule
	{
		[KSPField(isPersistant = false)]
		public string cryoModuleID = "CryoModule";

		ModuleSystemHeatCryoTank cryoModule;

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
			cryoModule = SystemHeatCryoResourceSim.FindCryoModule(part, cryoModuleID);
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (!CryoSettings.Enabled)
				return SystemHeatCryoResourceSim.BrokerTitle;

			if (cryoModule == null)
				cryoModule = SystemHeatCryoResourceSim.FindCryoModule(part, cryoModuleID);

			return SystemHeatCryoResourceSim.UpdateLoaded(cryoModule);
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			// SystemHeat cryo cooling is handled by the heat loop, not Kerbalism EC simulation.
			return SystemHeatCryoResourceSim.BrokerTitle;
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
			if (!CryoSettings.Enabled)
				return SystemHeatCryoResourceSim.BrokerTitle;

			SystemHeatCryoTankKerbalismUpdater updaterPrefab = proto_part_module as SystemHeatCryoTankKerbalismUpdater;
			string moduleId = updaterPrefab != null ? updaterPrefab.cryoModuleID : "CryoModule";

			ModuleSystemHeatCryoTank cryoPrefab = SystemHeatCryoResourceSim.FindCryoModule(proto_part, moduleId);
			if (cryoPrefab == null)
				return SystemHeatCryoResourceSim.BrokerTitle;

			ProtoPartModuleSnapshot cryoSnapshot = FindCryoSnapshot(part_snapshot);
			if (cryoSnapshot == null)
				return SystemHeatCryoResourceSim.BrokerTitle;

			return SystemHeatCryoResourceSim.BackgroundUpdate(v, part_snapshot, cryoSnapshot, cryoPrefab, elapsed_s);
		}

		static ProtoPartModuleSnapshot FindCryoSnapshot(ProtoPartSnapshot part)
		{
			return part.modules.Find(m => m.moduleName == "ModuleSystemHeatCryoTank");
		}
	}
}
