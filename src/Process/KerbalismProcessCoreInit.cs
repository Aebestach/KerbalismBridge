using KerbalismBridge;

namespace KerbalismProcess
{
	/// <summary>
	/// Called by zKerbalismPluginHost after zKerbalismProcess.dll is loaded (requires Bridge + Kerbalism).
	/// </summary>
	public static class KerbalismProcessCoreInit
	{
		public static void Initialize()
		{
			KerbalismProcessHarmony.ApplyPatches();
		}
	}
}
