using UnityEngine;

namespace KerbalismDynamicRadiation
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalismDynamicRadiationInit : MonoBehaviour
	{
		void Start()
		{
			DynamicRadiationSettings.Load();
		}
	}
}
