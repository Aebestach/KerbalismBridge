namespace KerbalismCryo
{
	public static class KerbalismCryoCoreInit
	{
		public static void Initialize()
		{
			CryoSettings.Load();
			CryoHarmonyPatches.ApplyPatches();
		}
	}
}
