using System.Collections.Generic;
using KERBALISM;
using SimpleBoiloff;

namespace KerbalismCryo
{
	public class CryoTankKerbalismUpdater : PartModule, IKerbalismModule
	{
		ModuleCryoTank cryoModule;

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
			cryoModule = CryoUtils.FindCryoTankModule(part);
		}

		public void FixedUpdate()
		{
			if (!CryoSettings.Enabled || !Lib.IsFlight())
				return;

			if (cryoModule == null)
				cryoModule = CryoUtils.FindCryoTankModule(part);
			if (cryoModule == null)
				return;

			CryoTankResourceSim.UpdateLoaded(cryoModule, vessel);
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (!CryoSettings.Enabled)
				return CryoTankResourceSim.BrokerTitle;

			if (cryoModule == null)
				cryoModule = CryoUtils.FindCryoTankModule(part);
			if (cryoModule == null)
				return CryoTankResourceSim.BrokerTitle;

			CryoTankResourceSim.UpdateLoaded(cryoModule, vessel);
			return CryoTankResourceSim.BrokerTitle;
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (!CryoSettings.Enabled)
				return CryoTankResourceSim.BrokerTitle;

			if (cryoModule == null)
				cryoModule = CryoUtils.FindCryoTankModule(part);
			if (cryoModule == null)
				return CryoTankResourceSim.BrokerTitle;

			CryoTankResourceSim.AddPlannerRates(cryoModule, resourceChangeRequest);
			return CryoTankResourceSim.BrokerTitle;
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
				return CryoTankResourceSim.BrokerTitle;

			ModuleCryoTank cryoPrefab = CryoUtils.FindCryoTankModule(proto_part);
			if (cryoPrefab == null)
				return CryoTankResourceSim.BrokerTitle;

			ProtoPartModuleSnapshot cryoSnapshot = FindCryoTankSnapshot(part_snapshot);
			if (cryoSnapshot == null)
				return CryoTankResourceSim.BrokerTitle;

			double ecRate = 0.0;
			string title = CryoTankResourceSim.BackgroundUpdate(v, part_snapshot, cryoSnapshot, cryoPrefab, elapsed_s);

			bool coolingEnabled = Lib.Proto.GetBool(cryoSnapshot, "CoolingEnabled");
			if (coolingEnabled)
			{
				IList<BoiloffFuel> fuels = CryoTankAccess.GetFuels(cryoPrefab);
				if (fuels != null && cryoPrefab.CoolingCost > 0f)
				{
					foreach (BoiloffFuel fuel in fuels)
					{
						if (fuel == null || string.IsNullOrEmpty(fuel.fuelName))
							continue;

						ProtoPartResourceSnapshot protoFuel = CryoUtils.FindPartResource(part_snapshot, fuel.fuelName);
						if (protoFuel == null || protoFuel.amount <= double.Epsilon)
							continue;
						ecRate += cryoPrefab.CoolingCost * protoFuel.amount * 0.001;
					}
				}
			}

			if (ecRate > 0.0)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -ecRate));

			return title;
		}

		static ProtoPartModuleSnapshot FindCryoTankSnapshot(ProtoPartSnapshot part)
		{
			return part.modules.Find(m => m.moduleName == "ModuleCryoTank");
		}
	}
}
