namespace KerbalismNFE
{
	/// <summary>
	/// Called by zKerbalismPluginHost after zKerbalismNFE.dll is loaded and Kerbalism is present.
	/// </summary>
	public static class KerbalismNFECoreInit
	{
		public static void Initialize()
		{
			KerbalismNFEHarmony.ApplyPatches();
		}
	}
}
