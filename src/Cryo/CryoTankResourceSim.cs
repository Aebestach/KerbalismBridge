using System.Collections;
using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;

namespace KerbalismCryo
{
	internal static class CryoTankResourceSim
	{
		internal const string BrokerName = "CryoTank";
		internal static string BrokerTitle => Localizer.Format("#LOC_KerbalismCryo_Brokers_Cryotank");

		internal static void AddPlannerRates(PartModule cryoModule, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (cryoModule == null)
				return;

			bool coolingEnabled = Lib.ReflectionValue<bool>(cryoModule, "CoolingEnabled");
			if (!coolingEnabled)
				return;

			float coolingCost = Lib.ReflectionValue<float>(cryoModule, "CoolingCost");
			IList fuels = CryoUtils.GetFuelsList(cryoModule);
			if (fuels == null || coolingCost <= 0f)
				return;

			double totalCost = 0.0;
			foreach (object fuel in fuels)
			{
				string fuelName = CryoUtils.GetFuelName(fuel);
				if (string.IsNullOrEmpty(fuelName))
					continue;

				double amount = Lib.Amount(cryoModule.part, fuelName);
				if (amount > double.Epsilon)
					totalCost += coolingCost * amount * 0.001;
			}

			if (totalCost > 0.0)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -totalCost));
		}

		internal static string UpdateLoaded(PartModule cryoModule, Vessel v)
		{
			if (cryoModule == null || v == null)
				return BrokerTitle;

			bool coolingEnabled = Lib.ReflectionValue<bool>(cryoModule, "CoolingEnabled");
			float coolingCost = Lib.ReflectionValue<float>(cryoModule, "CoolingCost");
			IList fuels = CryoUtils.GetFuelsList(cryoModule);
			if (fuels == null)
				return BrokerTitle;

			KERBALISM.ResourceBroker broker = KERBALISM.ResourceBroker.GetOrCreate(BrokerName, KERBALISM.ResourceBroker.BrokerCategory.VesselSystem, BrokerTitle);
			ResourceInfo ec = KERBALISM.ResourceCache.GetResource(v, "ElectricCharge");
			double dt = TimeWarp.fixedDeltaTime;
			double totalCost = 0.0;

			foreach (object fuel in fuels)
			{
				string fuelName = CryoUtils.GetFuelName(fuel);
				if (string.IsNullOrEmpty(fuelName))
					continue;

				PartResource resource = cryoModule.part.Resources.Get(fuelName);
				if (resource == null || resource.amount <= double.Epsilon)
					continue;

				if (coolingEnabled && coolingCost > 0f)
				{
					totalCost += coolingCost * resource.amount * 0.001 * dt;
				}
				else
				{
					float boiloffRate = CryoUtils.GetBoiloffRate(fuel);
					double boiled = CryoUtils.ApplyBoiloffAmount(resource.amount, boiloffRate, dt);
					if (boiled > double.Epsilon)
						resource.amount = (float)(resource.amount - boiled);
				}
			}

			if (coolingEnabled && totalCost > double.Epsilon)
			{
				if (ec.Amount < totalCost)
					Lib.ReflectionValue(cryoModule, "CoolingEnabled", false);
				else
					ec.Consume(totalCost, broker);
			}

			return BrokerTitle;
		}

		internal static string BackgroundUpdate(
			Vessel v,
			ProtoPartSnapshot part,
			ProtoPartModuleSnapshot cryoSnapshot,
			PartModule cryoPrefab,
			double elapsed_s)
		{
			if (cryoPrefab == null || part == null)
				return BrokerTitle;

			bool coolingEnabled = Lib.Proto.GetBool(cryoSnapshot, "CoolingEnabled");
			float coolingCost = Lib.ReflectionValue<float>(cryoPrefab, "CoolingCost");
			IList fuels = CryoUtils.GetFuelsList(cryoPrefab);
			if (fuels == null)
				return BrokerTitle;

			ResourceInfo ec = KERBALISM.ResourceCache.Get(v).GetResource(v, "ElectricCharge");
			bool coolingAvailable = coolingEnabled && ec.Amount > double.Epsilon;
			double totalEcCost = 0.0;
			string brokerTitle = BrokerTitle;

			foreach (object fuel in fuels)
			{
				string fuelName = CryoUtils.GetFuelName(fuel);
				if (string.IsNullOrEmpty(fuelName))
					continue;

				ProtoPartResourceSnapshot protoFuel = CryoUtils.FindPartResource(part, fuelName);
				if (protoFuel == null || protoFuel.amount <= double.Epsilon)
					continue;

				double amount = protoFuel.amount;

				if (coolingAvailable && coolingCost > 0f)
				{
					totalEcCost += coolingCost * amount * 0.001;
				}
				else
				{
					float boiloffRate = CryoUtils.GetBoiloffRate(fuel);
					double boiled = CryoUtils.ApplyBoiloffAmount(amount, boiloffRate, elapsed_s);
					CryoUtils.ConsumePartResource(part, fuelName, boiled, v, brokerTitle);
				}
			}

			if (totalEcCost > 0.0)
			{
				double ecNeed = totalEcCost * elapsed_s;
				if (ec.Amount < ecNeed)
					Lib.Proto.Set(cryoSnapshot, "CoolingEnabled", false);
			}

			return brokerTitle;
		}
	}
}
