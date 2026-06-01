using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalismResourceAudit
{
	static class ResourceAuditScanner
	{
		public static List<AuditFinding> Scan()
		{
			var findings = new List<AuditFinding>();

			if (!ResourceAuditSettings.Enabled)
				return findings;

			foreach (AvailablePart ap in PartLoader.LoadedPartsList)
			{
				if (ap?.partPrefab == null)
					continue;

				if (ap.partPrefab.Modules.Count == 0)
					continue;

				if (ap.name.StartsWith("kerbalEVA", StringComparison.Ordinal))
					continue;

				string modFolder = PartIntegrationRules.ExtractModFolder(ap);
				if (ResourceAuditSettings.IgnoreModFolders.Contains(modFolder))
					continue;

				if (ResourceAuditSettings.IgnorePartNames.Contains(ap.name))
					continue;

				var moduleNamesOnPart = new HashSet<string>(StringComparer.Ordinal);
				foreach (PartModule m in ap.partPrefab.Modules)
					moduleNamesOnPart.Add(m.moduleName);

				foreach (PartModule module in ap.partPrefab.Modules)
				{
					if (PartIntegrationRules.IsIntegrationModule(module, moduleNamesOnPart))
						continue;

					if (!PartIntegrationRules.IsSuspiciousModule(module))
						continue;

					findings.Add(new AuditFinding
					{
						ModFolder = modFolder,
						PartName = ap.name,
						PartTitle = ap.title,
						PartUrl = ap.partUrl ?? string.Empty,
						ModuleName = module.moduleName,
						ModuleType = module.GetType().FullName,
						Resources = PartIntegrationRules.DescribeResources(module),
					});
				}
			}

			return findings;
		}
	}
}
