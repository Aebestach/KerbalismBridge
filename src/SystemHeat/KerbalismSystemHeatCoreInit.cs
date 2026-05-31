namespace KerbalismSystemHeat
{
	/// <summary>
	/// Called by zKerbalismPluginHost after zKerbalismSystemHeat.dll is loaded and Kerbalism is present.
	/// </summary>
	public static class KerbalismSystemHeatCoreInit
	{
		public static void Initialize()
		{
			KerbalismSystemHeatHarmony.ApplyPatches();
		}
	}
}
