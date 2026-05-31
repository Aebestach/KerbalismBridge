using System.Collections.Generic;
using KERBALISM;
using NearFutureElectrical;
using UnityEngine;

namespace KerbalismNFE
{
	internal static class CapacitorResourceSim
	{
		internal static bool IsCharging(DischargeCapacitor capacitor)
		{
			return capacitor != null
				&& capacitor.Enabled
				&& !capacitor.Discharging
				&& capacitor.CurrentCharge < capacitor.MaximumCharge;
		}

		internal static bool IsDischarging(DischargeCapacitor capacitor)
		{
			return capacitor != null
				&& capacitor.Discharging
				&& capacitor.CurrentCharge > 1e-6f;
		}

		internal static void AddPlannerRates(DischargeCapacitor capacitor, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (IsDischarging(capacitor))
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", capacitor.dischargeActual));
			else if (IsCharging(capacitor))
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -capacitor.ChargeRate));
		}

		internal static string UpdateLoaded(DischargeCapacitor capacitor, Vessel v, string brokerName, string brokerTitle)
		{
			if (capacitor == null || v == null)
				return brokerTitle;

			capacitor.dischargeActual = Mathf.Clamp(
				capacitor.dischargeActual,
				capacitor.DischargeRate * capacitor.DischargeRateMinimumScalar,
				capacitor.DischargeRate);

			float dt = TimeWarp.fixedDeltaTime;
			KERBALISM.ResourceBroker broker = KERBALISM.ResourceBroker.GetOrCreate(brokerName, KERBALISM.ResourceBroker.BrokerCategory.Converter, brokerTitle);

			if (IsDischarging(capacitor))
			{
				double request = capacitor.dischargeActual * dt;
				double removed = RemoveStoredCharge(capacitor.part, request);
				if (removed > double.Epsilon)
					KERBALISM.ResourceCache.GetResource(v, "ElectricCharge").Produce(removed, broker);

				if (capacitor.DischargeGeneratesHeat && TimeWarp.CurrentRate <= 100f)
					capacitor.part.AddThermalFlux(capacitor.HeatRate);

				if (capacitor.CurrentCharge <= 1e-6f)
					capacitor.Discharging = false;
			}
			else if (IsCharging(capacitor))
			{
				double request = capacitor.ChargeRate * dt;
				ResourceInfo ec = KERBALISM.ResourceCache.GetResource(v, "ElectricCharge");
				if (ec.Amount >= request)
				{
					ec.Consume(request, broker);
					AddStoredCharge(capacitor.part, request * capacitor.ChargeRatio, capacitor.MaximumCharge);
					capacitor.lastUpdateTime = Planetarium.GetUniversalTime();
				}
			}

			UpdateCapacitorStatus(capacitor);
			SyncColorChanger(capacitor);
			return brokerTitle;
		}

		private static void UpdateCapacitorStatus(DischargeCapacitor capacitor)
		{
			if (capacitor == null)
				return;

			capacitor.dischargeActual = Mathf.Clamp(
				capacitor.dischargeActual,
				capacitor.DischargeRate * capacitor.DischargeRateMinimumScalar,
				capacitor.DischargeRate);

			if (IsDischarging(capacitor))
			{
				capacitor.CapacitorStatus = KSP.Localization.Localizer.Format(
					"#LOC_NFElectrical_ModuleDischargeCapacitor_Field_Status_Discharging",
					capacitor.dischargeActual.ToString("F2"));
			}
			else if (IsCharging(capacitor))
			{
				capacitor.CapacitorStatus = KSP.Localization.Localizer.Format(
					"#LOC_NFElectrical_ModuleDischargeCapacitor_Field_Status_Charging",
					capacitor.ChargeRate.ToString("F2"));
			}
			else if (capacitor.Enabled && !capacitor.Discharging && capacitor.CurrentCharge < capacitor.MaximumCharge)
			{
				capacitor.CapacitorStatus = KSP.Localization.Localizer.Format(
					"#LOC_NFElectrical_ModuleDischargeCapacitor_Field_Status_NoPower");
			}
			else if (capacitor.CurrentCharge <= 1e-6f)
			{
				capacitor.CapacitorStatus = KSP.Localization.Localizer.Format(
					"#LOC_NFElectrical_ModuleDischargeCapacitor_Field_Status_Empty");
			}
			else
			{
				capacitor.CapacitorStatus = KSP.Localization.Localizer.Format(
					"#LOC_NFElectrical_ModuleDischargeCapacitor_Field_Status_Ready");
			}
		}

		private static void SyncColorChanger(DischargeCapacitor capacitor)
		{
			if (capacitor == null || string.IsNullOrEmpty(capacitor.ModuleID) || capacitor.MaximumCharge <= 0f)
				return;

			foreach (ModuleColorChanger colorChanger in capacitor.part.GetComponents<ModuleColorChanger>())
			{
				if (colorChanger.moduleID == capacitor.ModuleID)
				{
					colorChanger.SetScalar(capacitor.CurrentCharge / capacitor.MaximumCharge);
					break;
				}
			}
		}

		internal static void SyncCapacitorVisuals(DischargeCapacitor capacitor)
		{
			UpdateCapacitorStatus(capacitor);
			SyncColorChanger(capacitor);
		}

		internal static string BackgroundUpdate(
			Vessel v,
			ProtoPartSnapshot partSnapshot,
			ProtoPartModuleSnapshot capacitorSnapshot,
			Part prefab,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			double elapsed_s)
		{
			if (Lib.Proto.GetBool(capacitorSnapshot, "Discharging"))
			{
				float dischargeRate = Lib.Proto.GetFloat(capacitorSnapshot, "dischargeActual");
				if (dischargeRate > 0f)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", dischargeRate));
					double removed = RemoveStoredCharge(partSnapshot, dischargeRate * elapsed_s);
					if (GetStoredCharge(partSnapshot) <= 1e-6)
						Lib.Proto.Set(capacitorSnapshot, "Discharging", false);
					else if (removed <= double.Epsilon)
						Lib.Proto.Set(capacitorSnapshot, "Discharging", false);
				}
			}
			else if (Lib.Proto.GetBool(capacitorSnapshot, "Enabled") && !Lib.Proto.GetBool(capacitorSnapshot, "Discharging"))
			{
				DischargeCapacitor prefabModule = prefab.FindModuleImplementing<DischargeCapacitor>();
				if (prefabModule == null || prefabModule.ChargeRate <= 0f)
					return NFECapacitorKerbalismUpdater.brokerTitle;

				float maximumCharge = prefabModule.MaximumCharge;
				if (GetStoredCharge(partSnapshot) >= maximumCharge)
					return NFECapacitorKerbalismUpdater.brokerTitle;

				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -prefabModule.ChargeRate));

				double ec = KERBALISM.ResourceCache.Get(v).GetResource(v, "ElectricCharge").Amount;
				double chargeRequest = prefabModule.ChargeRate * elapsed_s;
				if (ec >= chargeRequest)
					AddStoredCharge(partSnapshot, chargeRequest * prefabModule.ChargeRatio, maximumCharge);
			}

			return NFECapacitorKerbalismUpdater.brokerTitle;
		}

		private static PartResource FindStoredChargeResource(Part part)
		{
			if (part == null)
				return null;

			for (int i = 0; i < part.Resources.Count; i++)
			{
				PartResource resource = part.Resources[i];
				if (resource.resourceName == "StoredCharge")
					return resource;
			}
			return null;
		}

		private static double RemoveStoredCharge(Part part, double amount)
		{
			PartResource stored = FindStoredChargeResource(part);
			if (stored == null || amount <= 0.0)
				return 0.0;

			double removed = System.Math.Min(stored.amount, amount);
			stored.amount -= removed;
			return removed;
		}

		private static void AddStoredCharge(Part part, double amount, float maximumCharge)
		{
			PartResource stored = FindStoredChargeResource(part);
			if (stored == null || amount <= 0.0)
				return;

			stored.amount = System.Math.Min(maximumCharge, stored.amount + amount);
		}

		private static double GetStoredCharge(ProtoPartSnapshot partSnapshot)
		{
			ProtoPartResourceSnapshot stored = FindResource(partSnapshot, "StoredCharge");
			return stored != null ? stored.amount : 0.0;
		}

		private static double RemoveStoredCharge(ProtoPartSnapshot partSnapshot, double amount)
		{
			ProtoPartResourceSnapshot stored = FindResource(partSnapshot, "StoredCharge");
			if (stored == null || amount <= 0.0)
				return 0.0;

			double removed = System.Math.Min(stored.amount, amount);
			stored.amount -= removed;
			return removed;
		}

		private static void AddStoredCharge(ProtoPartSnapshot partSnapshot, double amount, float maximumCharge)
		{
			ProtoPartResourceSnapshot stored = FindResource(partSnapshot, "StoredCharge");
			if (stored == null || amount <= 0.0)
				return;

			stored.amount = System.Math.Min(maximumCharge, stored.amount + amount);
		}

		private static ProtoPartResourceSnapshot FindResource(ProtoPartSnapshot partSnapshot, string resourceName)
		{
			for (int i = 0; i < partSnapshot.resources.Count; i++)
			{
				if (partSnapshot.resources[i].resourceName == resourceName)
					return partSnapshot.resources[i];
			}
			return null;
		}
	}
}
