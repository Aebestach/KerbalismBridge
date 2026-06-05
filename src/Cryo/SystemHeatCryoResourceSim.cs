using System;
using System.Collections;
using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;
using SystemHeat;

namespace KerbalismCryo
{
	internal static class SystemHeatCryoResourceSim
	{
		internal const string BrokerName = "SystemHeatCryoTank";
		internal static string BrokerTitle => Localizer.Format("#LOC_KerbalismCryo_Brokers_SystemHeatCryotank");

		internal static ModuleSystemHeatCryoTank FindCryoModule(Part part, string moduleId)
		{
			if (part == null)
				return null;

			ModuleSystemHeatCryoTank[] modules = part.GetComponents<ModuleSystemHeatCryoTank>();
			if (modules == null || modules.Length == 0)
				return null;

			if (string.IsNullOrEmpty(moduleId))
				return modules[0];

			foreach (ModuleSystemHeatCryoTank module in modules)
			{
				if (module.moduleID == moduleId)
					return module;
			}

			return modules[0];
		}

		internal static void AddPlannerHeatRates(ModuleSystemHeatCryoTank cryo, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (cryo == null || !cryo.CoolingEnabled || !cryo.CoolingAllowed)
				return;

			double heatKw = EstimateCoolingHeatKw(cryo);
			if (heatKw > 0.0)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("SystemHeat", heatKw));
		}

		internal static double EstimateCoolingHeatKw(ModuleSystemHeatCryoTank cryo)
		{
			if (cryo == null)
				return 0.0;

			IList fuels = CryoUtils.GetFuelsList(cryo);
			if (fuels == null)
				return 0.0;

			double fuelAmount = 0.0;
			double heatCost = 0.0;
			foreach (object fuel in fuels)
			{
				string fuelName = CryoUtils.GetFuelName(fuel);
				if (string.IsNullOrEmpty(fuelName))
					continue;

				PartResource resource = cryo.part.Resources.Get(fuelName);
				if (resource == null || resource.amount <= double.Epsilon)
					continue;

				fuelAmount += resource.amount;
				float entryCost = CryoUtils.GetCoolingHeatCost(fuel);
				if (entryCost > 0f)
					heatCost = Math.Max(heatCost, entryCost);
			}

			if (heatCost <= 0f)
				heatCost = cryo.CoolingHeatCost;

			return heatCost * fuelAmount * 0.001;
		}

		internal static string UpdateLoaded(ModuleSystemHeatCryoTank cryo)
		{
			if (cryo == null)
				return BrokerTitle;

			// Native SystemHeat cryo tank drives loop heat and boiloff when loaded.
			return BrokerTitle;
		}

		internal static string BackgroundUpdate(
			Vessel v,
			ProtoPartSnapshot part,
			ProtoPartModuleSnapshot cryoSnapshot,
			ModuleSystemHeatCryoTank cryoPrefab,
			double elapsed_s)
		{
			if (cryoPrefab == null || part == null || elapsed_s <= 0.0)
				return BrokerTitle;

			bool coolingEnabled = Lib.Proto.GetBool(cryoSnapshot, "CoolingEnabled");
			bool coolingAllowed = Lib.Proto.GetBool(cryoSnapshot, "CoolingAllowed");
			IList fuels = CryoUtils.GetFuelsList(cryoPrefab);
			if (fuels == null)
				return BrokerTitle;

			double fluxScale = 1.0;
			double fuelAmount = 0.0;

			foreach (object fuel in fuels)
			{
				string fuelName = CryoUtils.GetFuelName(fuel);
				if (string.IsNullOrEmpty(fuelName))
					continue;

				ProtoPartResourceSnapshot protoFuel = CryoUtils.FindPartResource(part, fuelName);
				if (protoFuel == null || protoFuel.amount <= double.Epsilon)
					continue;

				fuelAmount += protoFuel.amount;
			}

			if (fuelAmount <= double.Epsilon)
				return BrokerTitle;

			SystemHeatBackgroundBridge.TryRun(v, elapsed_s);
			float loopTemp = GetLoopTemperature(part, cryoPrefab.systemHeatModuleID, v);

			bool allFuelsBoiloff = !coolingAllowed || !coolingEnabled;
			bool boiloffOccuring = false;

			foreach (object fuel in fuels)
			{
				string fuelName = CryoUtils.GetFuelName(fuel);
				if (string.IsNullOrEmpty(fuelName))
					continue;

				ProtoPartResourceSnapshot protoFuel = CryoUtils.FindPartResource(part, fuelName);
				if (protoFuel == null || protoFuel.amount <= double.Epsilon)
					continue;

				float cryoTemp = CryoUtils.GetCryoTemperature(fuel);
				bool fuelShouldBoiloff = allFuelsBoiloff || (cryoTemp > 0f && loopTemp > cryoTemp);
				if (!fuelShouldBoiloff)
					continue;

				float boiloffRate = CryoUtils.GetBoiloffRate(fuel);
				double boiled = CryoUtils.ApplyBoiloffAmountSystemHeat(protoFuel.amount, boiloffRate, elapsed_s, fluxScale);
				CryoUtils.ConsumePartResource(part, fuelName, boiled, v, BrokerTitle);
				boiloffOccuring = true;
			}

			Lib.Proto.Set(cryoSnapshot, "BoiloffOccuring", boiloffOccuring);
			return BrokerTitle;
		}

		static float GetLoopTemperature(ProtoPartSnapshot part, string systemHeatModuleId, Vessel v)
		{
			foreach (ProtoPartModuleSnapshot module in part.modules)
			{
				if (module.moduleName != "ModuleSystemHeat")
					continue;

				string moduleId = Lib.Proto.GetString(module, "moduleID");
				if (!string.IsNullOrEmpty(systemHeatModuleId) && moduleId != systemHeatModuleId)
					continue;

				float temp = Lib.Proto.GetFloat(module, "currentLoopTemperature");
				if (temp > 0f)
					return temp;
			}

			return 300f;
		}
	}
}
