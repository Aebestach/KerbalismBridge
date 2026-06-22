using System.Collections.Generic;
using System.Reflection;
using SimpleBoiloff;

namespace KerbalismCryo
{
	/// <summary>
	/// Access to <see cref="ModuleCryoTank"/> members not exposed by CryoTanks.
	/// The fuels list is private in SimpleBoiloff; one cached FieldInfo is the minimum needed.
	/// </summary>
	internal static class CryoTankAccess
	{
		private static readonly FieldInfo FuelsField =
			typeof(ModuleCryoTank).GetField("fuels", BindingFlags.Instance | BindingFlags.NonPublic);

		internal static IList<BoiloffFuel> GetFuels(ModuleCryoTank tank)
		{
			if (tank == null || FuelsField == null)
				return null;

			return FuelsField.GetValue(tank) as IList<BoiloffFuel>;
		}
	}
}
