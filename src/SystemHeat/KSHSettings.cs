using UnityEngine;

namespace KerbalismSystemHeat
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class KerbalismSystemHeatSettingsLoader : MonoBehaviour
	{
		private void Awake()
		{
			Load();
		}

		internal static void Load()
		{
			ConfigNode settingsNode = GameDatabase.Instance.GetConfigNode("zKerbalismSystemHeat/KERBALISMSYSTEMHEAT_SETTINGS");
			if (settingsNode == null)
				return;

			string enabled = settingsNode.GetValue("BackgroundThermalSim");
			if (!string.IsNullOrEmpty(enabled))
				bool.TryParse(enabled, out SystemHeatBackgroundThermal.Enabled);

			settingsNode.TryGetValue("BackgroundRadiatorCoefficient", ref SystemHeatBackgroundThermal.RadiatorCoefficient);
		}
	}
}
