using System;
using System.Collections;
using System.Reflection;
using KERBALISM;

namespace KerbalismCryo
{
	internal static class CryoUtils
	{
		internal static void Log(string message)
		{
			Lib.Log("[zKerbalismCryo] " + message);
		}

		internal static void LogError(string message)
		{
			Lib.Log("[zKerbalismCryo] ERROR: " + message);
		}

		internal static bool PartHasCryoUpdater(ProtoPartSnapshot part)
		{
			if (part == null)
				return false;

			foreach (ProtoPartModuleSnapshot module in part.modules)
			{
				if (module.moduleName == "CryoTankKerbalismUpdater"
					|| module.moduleName == "SystemHeatCryoTankKerbalismUpdater")
					return true;
			}

			return false;
		}

		internal static PartModule FindCryoTankModule(Part part)
		{
			if (part == null)
				return null;

			foreach (PartModule module in part.Modules)
			{
				if (module.moduleName == "ModuleCryoTank")
					return module;
			}

			return null;
		}

		internal static Type ResolveSystemHeatCryoTankType()
		{
			return Type.GetType("SystemHeat.ModuleSystemHeatCryoTank, SystemHeat", false);
		}

		internal static object GetFieldValue(object target, string fieldName)
		{
			if (target == null)
				return null;

			FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			return field?.GetValue(target);
		}

		internal static T GetFieldValue<T>(object target, string fieldName, T fallback = default)
		{
			object value = GetFieldValue(target, fieldName);
			if (value is T typed)
				return typed;
			return fallback;
		}

		internal static IList GetFuelsList(PartModule cryoModule)
		{
			return GetFieldValue(cryoModule, "fuels") as IList;
		}

		internal static string GetFuelName(object fuelEntry)
		{
			return GetFieldValue<string>(fuelEntry, "fuelName");
		}

		internal static float GetBoiloffRate(object fuelEntry)
		{
			return GetFieldValue<float>(fuelEntry, "boiloffRate");
		}

		internal static float GetCryoTemperature(object fuelEntry)
		{
			float temp = GetFieldValue<float>(fuelEntry, "cryoTemperature");
			if (temp > 0f)
				return temp;
			return GetFieldValue<float>(fuelEntry, "CryocoolerTemperature");
		}

		internal static float GetCoolingHeatCost(object fuelEntry)
		{
			float value = GetFieldValue<float>(fuelEntry, "coolingHeatCost");
			if (value > 0f)
				return value;
			return GetFieldValue<float>(fuelEntry, "CoolingHeatCost");
		}

		internal static double ApplyBoiloffAmount(double amount, float boiloffRatePercentPerHour, double elapsed_s)
		{
			double boiloffRate = boiloffRatePercentPerHour / 360000.0;
			return amount * (1.0 - Math.Pow(1.0 - boiloffRate, elapsed_s));
		}

		internal static double ApplyBoiloffAmountSystemHeat(double amount, float boiloffRatePercentPerHour, double elapsed_s, double scale)
		{
			double boiloffRateSeconds = boiloffRatePercentPerHour / 100.0 / 3600.0;
			return amount * (1.0 - Math.Pow(1.0 - boiloffRateSeconds, elapsed_s)) * scale;
		}

		internal static ProtoPartResourceSnapshot FindPartResource(ProtoPartSnapshot part, string resourceName)
		{
			return part.resources.Find(r => r.resourceName == resourceName);
		}

		internal static void ConsumePartResource(ProtoPartSnapshot part, string resourceName, double amount, Vessel v, string brokerTitle)
		{
			if (amount <= 0.0)
				return;

			ProtoPartResourceSnapshot proto = FindPartResource(part, resourceName);
			if (proto == null)
				return;

			double removed = Math.Min(proto.amount, amount);
			proto.amount -= removed;

			ResourceInfo vesselResource = KERBALISM.ResourceCache.GetResource(v, resourceName);
			if (vesselResource.Amount >= removed)
				vesselResource.Consume(removed, KERBALISM.ResourceBroker.GetOrCreate("CryoTank", KERBALISM.ResourceBroker.BrokerCategory.VesselSystem, brokerTitle));
		}
	}
}
