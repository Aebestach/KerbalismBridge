using System;
using System.Collections.Generic;
using System.Reflection;
using KERBALISM;

namespace KerbalismResourceAudit
{
	static class PartIntegrationRules
	{
		static readonly Type[] ResourceUpdateSignature =
		{
			typeof(Dictionary<string, double>),
			typeof(List<KeyValuePair<string, double>>),
		};

		public static bool IsIntegrationModule(PartModule module, HashSet<string> moduleNamesOnPart)
		{
			if (module == null)
				return true;

			string moduleName = module.moduleName;
			if (ResourceAuditSettings.IntegrationModuleNames.Contains(moduleName))
				return true;

			foreach (string suffix in ResourceAuditSettings.IntegrationModuleSuffixes)
			{
				if (moduleName.EndsWith(suffix, StringComparison.Ordinal))
					return true;
			}

			if (module is IKerbalismModule)
				return true;

			if (HasResourceUpdate(module))
				return true;

			if (IsNativeModuleCoveredByUpdater(moduleName, moduleNamesOnPart))
				return true;

			return false;
		}

		public static bool IsSuspiciousModule(PartModule module)
		{
			if (module == null)
				return false;

			return ResourceAuditSettings.SuspiciousModuleNames.Contains(module.moduleName);
		}

		static bool HasResourceUpdate(PartModule module)
		{
			MethodInfo method = module.GetType().GetMethod(
				"ResourceUpdate",
				BindingFlags.Instance | BindingFlags.Public,
				null,
				ResourceUpdateSignature,
				null);

			return method != null;
		}

		static bool IsNativeModuleCoveredByUpdater(string nativeModuleName, HashSet<string> moduleNamesOnPart)
		{
			foreach (string pair in ResourceAuditSettings.NativeModuleUpdaterPairs)
			{
				int sep = pair.IndexOf('|');
				if (sep < 0)
					continue;

				string native = pair.Substring(0, sep);
				string updater = pair.Substring(sep + 1);

				if (!native.Equals(nativeModuleName, StringComparison.Ordinal))
					continue;

				if (moduleNamesOnPart.Contains(updater))
					return true;
			}

			return false;
		}

		public static string ExtractModFolder(AvailablePart ap)
		{
			string url = ap?.partUrl;
			if (string.IsNullOrEmpty(url))
				return "Unknown";

			url = url.Replace('\\', '/');
			const string marker = "GameData/";
			int idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
			if (idx >= 0)
				url = url.Substring(idx + marker.Length);

			int slash = url.IndexOf('/');
			if (slash > 0)
				return url.Substring(0, slash);

			return url;
		}

		public static string DescribeResources(PartModule module)
		{
			if (module == null)
				return string.Empty;

			var converter = module as ModuleResourceConverter;
			if (converter != null)
				return DescribeConverter(converter);

			var harvester = module as ModuleResourceHarvester;
			if (harvester != null)
				return DescribeHarvester(harvester);

			var generator = module as ModuleGenerator;
			if (generator != null)
				return DescribeGenerator(generator);

			return string.Empty;
		}

		static string DescribeConverter(ModuleResourceConverter converter)
		{
			var parts = new List<string>();

			if (converter.inputList != null)
			{
				foreach (ResourceRatio ratio in converter.inputList)
					parts.Add("IN:" + ratio.ResourceName + "@" + ratio.Ratio);
			}

			if (converter.outputList != null)
			{
				foreach (ResourceRatio ratio in converter.outputList)
					parts.Add("OUT:" + ratio.ResourceName + "@" + ratio.Ratio);
			}

			if (parts.Count == 0 && !string.IsNullOrEmpty(converter.ConverterName))
				parts.Add("ConverterName=" + converter.ConverterName);

			return string.Join(" ", parts);
		}

		static string DescribeHarvester(ModuleResourceHarvester harvester)
		{
			var parts = new List<string>();

			if (harvester.inputList != null)
			{
				foreach (ResourceRatio ratio in harvester.inputList)
					parts.Add("IN:" + ratio.ResourceName + "@" + ratio.Ratio);
			}

			parts.Add("OUT:" + harvester.ResourceName + "@" + harvester.Efficiency);
			return string.Join(" ", parts);
		}

		static string DescribeGenerator(ModuleGenerator generator)
		{
			if (generator?.resHandler == null)
				return string.Empty;

			var parts = new List<string>();

			if (generator.resHandler.inputResources != null)
			{
				foreach (ModuleResource res in generator.resHandler.inputResources)
				{
					if (res == null)
						continue;
					parts.Add("IN:" + res.name + "@" + res.rate);
				}
			}

			if (generator.resHandler.outputResources != null)
			{
				foreach (ModuleResource res in generator.resHandler.outputResources)
				{
					if (res == null)
						continue;
					parts.Add("OUT:" + res.name + "@" + res.rate);
				}
			}

			return string.Join(" ", parts);
		}
	}
}
