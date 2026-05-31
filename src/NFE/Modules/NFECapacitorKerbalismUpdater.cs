using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;
using NearFutureElectrical;

namespace KerbalismNFE
{
	public class NFECapacitorKerbalismUpdater : PartModule, IKerbalismModule
	{
		public static string brokerName = "NFECapacitor";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismNFE_Brokers_Capacitor");

		protected DischargeCapacitor capacitorModule;
		private bool lastPlannerDischarging;
		private bool lastPlannerCharging;

		internal DischargeCapacitor CapacitorModule => capacitorModule;

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
			if (capacitorModule == null)
				capacitorModule = FindCapacitorModule(part);
		}

		public void FixedUpdate()
		{
			if (capacitorModule == null)
				capacitorModule = FindCapacitorModule(part);
			if (capacitorModule == null)
				return;

			if (Lib.IsFlight())
				RefreshPlannerIfStateChanged();
		}

		public void Update()
		{
			if (capacitorModule == null)
				capacitorModule = FindCapacitorModule(part);
			if (capacitorModule == null)
				return;

			if (Lib.IsEditor())
			{
				CapacitorResourceSim.SyncCapacitorVisuals(capacitorModule);
				RefreshPlannerIfStateChanged();
			}
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (capacitorModule == null)
				capacitorModule = FindCapacitorModule(part);
			return CapacitorResourceSim.UpdateLoaded(capacitorModule, vessel, brokerName, brokerTitle);
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (capacitorModule == null)
				capacitorModule = FindCapacitorModule(part);
			if (capacitorModule != null)
				CapacitorResourceSim.AddPlannerRates(capacitorModule, resourceChangeRequest);
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
			ProtoPartModuleSnapshot capacitor = KNFEUtils.FindPartModuleSnapshot(part_snapshot, "DischargeCapacitor");
			if (capacitor == null)
				return "ERR: no capacitor";

			return CapacitorResourceSim.BackgroundUpdate(v, part_snapshot, capacitor, proto_part, resourceChangeRequest, elapsed_s);
		}

		private void RefreshPlannerIfStateChanged()
		{
			if (capacitorModule == null)
				return;

			bool plannerDischarging = CapacitorResourceSim.IsDischarging(capacitorModule);
			bool plannerCharging = CapacitorResourceSim.IsCharging(capacitorModule);
			if (plannerDischarging == lastPlannerDischarging && plannerCharging == lastPlannerCharging)
				return;

			lastPlannerDischarging = plannerDischarging;
			lastPlannerCharging = plannerCharging;
			KNFEUtils.UpdateKerbalismPlannerUI();
		}

		internal static DischargeCapacitor FindCapacitorModule(Part part)
		{
			foreach (DischargeCapacitor capacitor in part.GetComponents<DischargeCapacitor>())
				return capacitor;
			return null;
		}
	}
}
