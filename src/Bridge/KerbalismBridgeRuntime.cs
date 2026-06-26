using System.Collections;
using UnityEngine;

namespace KerbalismBridge
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class KerbalismBridgeRuntime : MonoBehaviour
	{
		private void Start()
		{
			StartCoroutine(RegisterWhenReady());
		}

		private static IEnumerator RegisterWhenReady()
		{
			while (!KerbalismBridgeGameEvents.TryRegister())
				yield return null;
		}
	}
}
