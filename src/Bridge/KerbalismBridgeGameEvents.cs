using KERBALISM;

namespace KerbalismBridge
{
	/// <summary>
	/// Registers KSP GameEvents after MainMenu startup. Must not run from PluginHost Instantly init.
	/// </summary>
	internal static class KerbalismBridgeGameEvents
	{
		private static bool registered;

		internal static bool TryRegister()
		{
			if (registered)
				return true;

			if (GameEvents.onGameSceneSwitchRequested == null
				|| GameEvents.onGamePause == null
				|| GameEvents.onPartPack == null
				|| GameEvents.onVesselGoOnRails == null
				|| GameEvents.onVesselSwitching == null
				|| GameEvents.onVesselSwitchingToUnloaded == null)
				return false;

			try
			{
				GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);
				GameEvents.onGamePause.Add(OnGamePauseCapture);
				GameEvents.onPartPack.Add(OnPartPackCapture);
				GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRailsCapture);
				GameEvents.onVesselSwitching.Add(OnVesselSwitchingCapture);
				GameEvents.onVesselSwitchingToUnloaded.Add(OnVesselSwitchingCapture);
			}
			catch
			{
				return false;
			}

			registered = true;
			return true;
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

		// Anchor the last-good loaded loop temperature/flux at the on-rails (pack) transition, but ONLY
		// below the loaded-hyperwarp stabilization scope. onVesselGoOnRails can fire on a hyperwarp tick
		// whose loop temperature is already a stale-flux spike; capturing then would overwrite the good
		// anchor with that spike and make the stabilizer freeze to a corrupted value. At/above the
		// threshold we rely on the rolling sane anchor captured by the SystemHeatVessel postfix on
		// loaded+unpacked frames.
		private static void OnVesselGoOnRailsCapture(Vessel v)
		{
			if (HighLogic.LoadedSceneIsFlight && v != null && v.loaded
				&& TimeWarp.fixedDeltaTime < SystemHeatBackgroundThermal.LoadedHyperwarpStabilizeMinFixedDt)
				SystemHeatBackgroundThermal.CaptureLoadedTemperatures(v);
		}

		private static void OnVesselSwitchingCapture(Vessel from, Vessel to)
		{
			if (HighLogic.LoadedSceneIsFlight)
				SystemHeatBackgroundThermal.CaptureLoadedTemperatures(from);
		}
	}
}
