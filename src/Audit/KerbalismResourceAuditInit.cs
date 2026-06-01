using System.Collections;
using UnityEngine;

namespace KerbalismResourceAudit
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public sealed class KerbalismResourceAuditInit : MonoBehaviour
	{
		static bool scanCompleted;

		void Start()
		{
			if (scanCompleted)
				return;

			StartCoroutine(RunWhenReady());
		}

		IEnumerator RunWhenReady()
		{
			while (PartLoader.Instance == null)
				yield return null;

			// PartLoader has no public "done" flag in current KSP; wait until the database is populated.
			while (PartLoader.LoadedPartsList == null || PartLoader.LoadedPartsList.Count == 0)
				yield return null;

			// One frame after PartLoader so MM / Kerbalism prefab tweaks can settle.
			yield return null;

			ResourceAuditSettings.Load();

			if (!ResourceAuditSettings.Enabled)
			{
				if (ResourceAuditSettings.LogToUnity)
					Debug.Log("[zKerbalismResourceAudit] Scan disabled in Settings.cfg.");
				scanCompleted = true;
				yield break;
			}

			var findings = ResourceAuditScanner.Scan();
			string scanDir = ResourceAuditWriter.WriteScan(findings);

			scanCompleted = true;

			if (ResourceAuditSettings.LogToUnity)
			{
				Debug.Log(
					"[zKerbalismResourceAudit] Static scan complete. Findings: " + findings.Count +
					". Logs: " + scanDir);
			}
		}
	}
}
