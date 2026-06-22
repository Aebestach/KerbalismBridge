using KERBALISM;

namespace KerbalismBridge
{
	/// <summary>Called by zKerbalismPluginHost after zKerbalismBridge.dll is loaded and Kerbalism is present.</summary>
	public static class KerbalismBridgeCoreInit
	{
		public static void Initialize()
		{
			GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);
			GameEvents.onGamePause.Add(OnGamePauseCapture);
			BridgeUtils.Log("Kerbalism Bridge runtime loaded.");
		}

		private static void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> data)
		{
			if (data.from == GameScenes.FLIGHT)
				SystemHeatBackgroundThermal.CaptureAllLoadedFissionReactors();
		}

		private static void OnGamePauseCapture()
		{
			if (HighLogic.LoadedSceneIsFlight)
				SystemHeatBackgroundThermal.CaptureAllLoadedFissionReactors();
		}
	}
}
