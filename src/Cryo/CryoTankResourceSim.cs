using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;
using SimpleBoiloff;

namespace KerbalismCryo
{
	internal static class CryoTankResourceSim
	{
		internal const string BrokerName = "CryoTank";
		internal static string BrokerTitle => Localizer.Format("#LOC_KerbalismCryo_Brokers_Cryotank");

		internal static void AddPlannerRates(ModuleCryoTank cryoModule, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (cryoModule == null || !cryoModule.CoolingEnabled)
				return;

			IList<BoiloffFuel> fuels = CryoTankAccess.GetFuels(cryoModule);
			if (fuels == null || cryoModule.CoolingCost <= 0f)
				return;

			double totalCost = 0.0;
			foreach (BoiloffFuel fuel in fuels)
			{
				if (fuel == null || string.IsNullOrEmpty(fuel.fuelName))
					continue;

				double amount = Lib.Amount(cryoModule.part, fuel.fuelName);
				if (amount > double.Epsilon)
					totalCost += cryoModule.CoolingCost * amount * 0.001;
			}

			if (totalCost > 0.0)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -totalCost));
		}

		internal static string UpdateLoaded(ModuleCryoTank cryoModule, Vessel v)
		{
			if (cryoModule == null || v == null)
				return BrokerTitle;

			IList<BoiloffFuel> fuels = CryoTankAccess.GetFuels(cryoModule);
			if (fuels == null)
				return BrokerTitle;

			KERBALISM.ResourceBroker broker = KERBALISM.ResourceBroker.GetOrCreate(BrokerName, KERBALISM.ResourceBroker.BrokerCategory.VesselSystem, BrokerTitle);
			ResourceInfo ec = KERBALISM.ResourceCache.GetResource(v, "ElectricCharge");
			double dt = TimeWarp.fixedDeltaTime;
			double totalCost = 0.0;

			foreach (BoiloffFuel fuel in fuels)
			{
				if (fuel == null || string.IsNullOrEmpty(fuel.fuelName))
					continue;

				PartResource resource = cryoModule.part.Resources.Get(fuel.fuelName);
				if (resource == null || resource.amount <= double.Epsilon)
					continue;

				if (cryoModule.CoolingEnabled && cryoModule.CoolingCost > 0f)
				{
					totalCost += cryoModule.CoolingCost * resource.amount * 0.001 * dt;
				}
				else
				{
					double boiled = CryoUtils.ApplyBoiloffAmount(resource.amount, fuel.boiloffRate, dt);
					if (boiled > double.Epsilon)
						resource.amount = (float)(resource.amount - boiled);
				}
			}

			if (cryoModule.CoolingEnabled && totalCost > double.Epsilon)
			{
				if (ec.Amount < totalCost)
					cryoModule.CoolingEnabled = false;
				else
					ec.Consume(totalCost, broker);
			}

			return BrokerTitle;
		}

		internal static string BackgroundUpdate(
			Vessel v,
			ProtoPartSnapshot part,
			ProtoPartModuleSnapshot cryoSnapshot,
			ModuleCryoTank cryoPrefab,
			double elapsed_s)
		{
			if (cryoPrefab == null || part == null)
				return BrokerTitle;

			bool coolingEnabled = Lib.Proto.GetBool(cryoSnapshot, "CoolingEnabled");
			IList<BoiloffFuel> fuels = CryoTankAccess.GetFuels(cryoPrefab);
			if (fuels == null)
				return BrokerTitle;

			ResourceInfo ec = KERBALISM.ResourceCache.Get(v).GetResource(v, "ElectricCharge");
			bool coolingAvailable = coolingEnabled && ec.Amount > double.Epsilon;
			double totalEcCost = 0.0;
			string brokerTitle = BrokerTitle;

			foreach (BoiloffFuel fuel in fuels)
			{
				if (fuel == null || string.IsNullOrEmpty(fuel.fuelName))
					continue;

				ProtoPartResourceSnapshot protoFuel = CryoUtils.FindPartResource(part, fuel.fuelName);
				if (protoFuel == null || protoFuel.amount <= double.Epsilon)
					continue;

				double amount = protoFuel.amount;

				if (coolingAvailable && cryoPrefab.CoolingCost > 0f)
				{
					totalEcCost += cryoPrefab.CoolingCost * amount * 0.001;
				}
				else
				{
					double boiled = CryoUtils.ApplyBoiloffAmount(amount, fuel.boiloffRate, elapsed_s);
					CryoUtils.ConsumePartResource(part, fuel.fuelName, boiled, v, brokerTitle);
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
