namespace KerbalismBridge
{
	/// <summary>
	/// Called by zKerbalismPluginHost after zKerbalismBridge.dll is loaded and Kerbalism is present.
	/// </summary>
	public static class KerbalismBridgeCoreInit
	{
		public static void Initialize()
		{
			BridgeUtils.Log("Kerbalism Bridge runtime loaded.");
		}
	}
}
