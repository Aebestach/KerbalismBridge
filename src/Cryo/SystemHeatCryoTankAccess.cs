using System.Collections;
using System.Reflection;
using SystemHeat;

namespace KerbalismCryo
{
	/// <summary>
	/// SystemHeat cryo tank fuel entries are not publicly exposed on <see cref="ModuleSystemHeatCryoTank"/>.
	/// </summary>
	internal static class SystemHeatCryoTankAccess
	{
		private static readonly FieldInfo FuelsField =
			typeof(ModuleSystemHeatCryoTank).GetField("fuels", BindingFlags.Instance | BindingFlags.NonPublic);

		internal static IEnumerable GetFuels(ModuleSystemHeatCryoTank tank)
		{
			if (tank == null || FuelsField == null)
				return null;

			return FuelsField.GetValue(tank) as IEnumerable;
		}

		internal static string GetFuelName(object fuelEntry)
		{
			return ReadField<string>(fuelEntry, "fuelName");
		}

		internal static float GetBoiloffRate(object fuelEntry)
		{
			return ReadField<float>(fuelEntry, "boiloffRate");
		}

		internal static float GetCryoTemperature(object fuelEntry)
		{
			float temp = ReadField<float>(fuelEntry, "cryoTemperature");
			if (temp > 0f)
				return temp;
			return ReadField<float>(fuelEntry, "CryocoolerTemperature");
		}

		internal static float GetCoolingHeatCost(object fuelEntry)
		{
			float value = ReadField<float>(fuelEntry, "coolingHeatCost");
			if (value > 0f)
				return value;
			return ReadField<float>(fuelEntry, "CoolingHeatCost");
		}

		private static T ReadField<T>(object target, string name, T fallback = default)
		{
			if (target == null)
				return fallback;

			FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null || !typeof(T).IsAssignableFrom(field.FieldType))
				return fallback;

			return (T)field.GetValue(target);
		}
	}
}
