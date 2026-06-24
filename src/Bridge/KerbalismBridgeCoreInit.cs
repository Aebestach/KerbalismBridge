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
			GameEvents.onPartPack.Add(OnPartPackCapture);
			GameEvents.onVesselSwitching.Add(OnVesselSwitchingCapture);
			GameEvents.onVesselSwitchingToUnloaded.Add(OnVesselSwitchingCapture);
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

		private static void OnPartPackCapture(Part part)
		{
			if (HighLogic.LoadedSceneIsFlight)
				SystemHeatBackgroundThermal.CaptureLoadedFissionReactorState(part);
		}

		private static void OnVesselSwitchingCapture(Vessel from, Vessel to)
		{
			if (HighLogic.LoadedSceneIsFlight)
				SystemHeatBackgroundThermal.CaptureLoadedTemperatures(from);
		}
	}
}
