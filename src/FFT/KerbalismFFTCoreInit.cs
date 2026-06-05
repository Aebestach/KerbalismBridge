namespace KerbalismFFT
{
	/// <summary>
	/// Called by zKerbalismPluginHost after zKerbalismFFT.dll is loaded and Kerbalism is present.
	/// </summary>
	public static class KerbalismFFTCoreInit
	{
		public static void Initialize()
		{
			FFTSettings.Load();
			KerbalismFFTHarmony.ApplyPatches();
		}
	}
}
