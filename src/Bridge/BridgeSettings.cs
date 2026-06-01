using UnityEngine;

namespace KerbalismBridge
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class KerbalismBridgeSettingsLoader : MonoBehaviour
	{
		private void Awake()
		{
			Load();
		}

		internal static void Load()
		{
			ConfigNode settingsNode = GameDatabase.Instance.GetConfigNode("zKerbalismBridge/KERBALISM_BRIDGE_SETTINGS");
			if (settingsNode == null)
				return;

			string enabled = settingsNode.GetValue("BackgroundThermalSim");
			if (!string.IsNullOrEmpty(enabled))
				bool.TryParse(enabled, out SystemHeatBackgroundThermal.Enabled);

			settingsNode.TryGetValue("BackgroundRadiatorCoefficient", ref SystemHeatBackgroundThermal.RadiatorCoefficient);
		}
	}
}
