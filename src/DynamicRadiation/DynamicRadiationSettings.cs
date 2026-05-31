using System;

namespace KerbalismDynamicRadiation
{
	static class DynamicRadiationSettings
	{
		const string SettingsNode = "KERBALISM_DYNAMIC_RADIATION_SETTINGS";

		public static double ReactorMinEmissionPercent = 25.0;
		public static double ReactorEmissionDecayRate = 3600.0;
		public static double EngineMinEmissionPercent = 25.0;
		public static double EngineEmissionDecayRate = 360.0;

		public static void Load()
		{
			ConfigNode node = GameDatabase.Instance.GetConfigNode(SettingsNode);
			if (node == null)
				return;

			ReactorMinEmissionPercent = Read(node, "Reactor_MinEmissionPercent", ReactorMinEmissionPercent);
			ReactorEmissionDecayRate = Read(node, "Reactor_EmissionDecayRate", ReactorEmissionDecayRate);
			EngineMinEmissionPercent = Read(node, "Engine_MinEmissionPercent", EngineMinEmissionPercent);
			EngineEmissionDecayRate = Read(node, "Engine_EmissionDecayRate", EngineEmissionDecayRate);
		}

		static double Read(ConfigNode node, string key, double fallback)
		{
			if (!node.HasValue(key))
				return fallback;

			if (double.TryParse(node.GetValue(key), out double value))
				return value;

			return fallback;
		}
	}
}
