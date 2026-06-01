using System;
using System.Collections.Generic;

namespace KerbalismResourceAudit
{
	static class ResourceAuditSettings
	{
		const string SettingsNode = "KERBALISM_RESOURCE_AUDIT_SETTINGS";

		public static bool Enabled = true;
		public static bool LogToUnity = true;

		public static readonly HashSet<string> SuspiciousModuleNames = new HashSet<string>(StringComparer.Ordinal);
		public static readonly HashSet<string> IntegrationModuleNames = new HashSet<string>(StringComparer.Ordinal);
		public static readonly HashSet<string> IntegrationModuleSuffixes = new HashSet<string>(StringComparer.Ordinal);
		public static readonly HashSet<string> NativeModuleUpdaterPairs = new HashSet<string>(StringComparer.Ordinal);
		public static readonly HashSet<string> IgnoreModFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		public static readonly HashSet<string> IgnorePartNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public static void Load()
		{
			ApplyDefaults();

			ConfigNode node = GameDatabase.Instance.GetConfigNode(SettingsNode);
			if (node == null)
				return;

			if (node.HasValue("Enabled"))
				Enabled = ParseBool(node.GetValue("Enabled"), Enabled);

			if (node.HasValue("LogToUnity"))
				LogToUnity = ParseBool(node.GetValue("LogToUnity"), LogToUnity);

			MergeList(node, "SuspiciousModule", SuspiciousModuleNames, replace: true);
			MergeList(node, "IntegrationModule", IntegrationModuleNames, replace: true);
			MergeList(node, "IntegrationModuleSuffix", IntegrationModuleSuffixes, replace: true);
			MergeList(node, "NativeModuleUpdaterPair", NativeModuleUpdaterPairs, replace: true);
			MergeList(node, "IgnoreMod", IgnoreModFolders, replace: false);
			MergeList(node, "IgnorePart", IgnorePartNames, replace: false);
		}

		static void ApplyDefaults()
		{
			SuspiciousModuleNames.Clear();
			string[] suspicious =
			{
				"ModuleResourceConverter",
				"ModuleResourceHarvester",
				"ModuleGenerator",
				"ModuleCryoTank",
				"ModuleScienceConverter",
				"ModuleFuelCell",
				"ModuleFuelCellArray",
				"ModuleSystemHeatConverter",
				"ModuleSystemHeatHarvester",
				"FusionReactor",
				"ModuleFusionEngine",
				"DischargeCapacitor",
				"ModuleRadioisotopeGenerator",
				"FissionGenerator",
				"ModuleKPBSConverter",
			};
			foreach (string s in suspicious)
				SuspiciousModuleNames.Add(s);

			IntegrationModuleNames.Clear();
			string[] integration =
			{
				"ProcessController",
				"ProcessControllerSystemHeat",
				"Harvester",
				"HarvesterSystemHeat",
				"KerbalismProcess",
				"Configure",
				"SolarPanelFixer",
			};
			foreach (string s in integration)
				IntegrationModuleNames.Add(s);

			IntegrationModuleSuffixes.Clear();
			IntegrationModuleSuffixes.Add("KerbalismUpdater");

			NativeModuleUpdaterPairs.Clear();
			string[] pairs =
			{
				"ModuleSystemHeatConverter|SystemHeatConverterKerbalismUpdater",
				"ModuleSystemHeatHarvester|SystemHeatHarvesterKerbalismUpdater",
				"ModuleSpaceDustHarvester|SpaceDustHarvesterKerbalismUpdater",
				"FusionReactor|FusionReactorKerbalismUpdater",
				"ModuleFusionEngine|FusionEngineKerbalismUpdater",
				"DischargeCapacitor|NFECapacitorKerbalismUpdater",
			};
			foreach (string s in pairs)
				NativeModuleUpdaterPairs.Add(s);

			IgnoreModFolders.Clear();
			IgnorePartNames.Clear();
		}

		static void MergeList(ConfigNode node, string key, HashSet<string> target, bool replace)
		{
			if (!node.HasValue(key))
				return;

			if (replace)
				target.Clear();

			foreach (string value in node.GetValues(key))
			{
				string trimmed = value.Trim();
				if (trimmed.Length > 0)
					target.Add(trimmed);
			}
		}

		static bool ParseBool(string raw, bool fallback)
		{
			if (string.IsNullOrEmpty(raw))
				return fallback;

			raw = raw.Trim();
			if (bool.TryParse(raw, out bool b))
				return b;

			if (raw == "1" || raw.Equals("yes", StringComparison.OrdinalIgnoreCase))
				return true;

			if (raw == "0" || raw.Equals("no", StringComparison.OrdinalIgnoreCase))
				return false;

			return fallback;
		}
	}
}
