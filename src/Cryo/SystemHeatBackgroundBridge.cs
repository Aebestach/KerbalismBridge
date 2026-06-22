using KerbalismBridge;

namespace KerbalismCryo
{
	internal static class SystemHeatBackgroundBridge
	{
		internal static void TryRun(Vessel v, double elapsed_s)
		{
			SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
		}
	}
}
