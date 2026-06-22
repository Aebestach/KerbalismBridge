using KERBALISM;
using UnityEngine;

namespace KerbalismBridge
{
	/// <summary>Optional diagnostics for fission reactor background transitions. Filter: Kerbalism.FissionReactorDbg</summary>
	internal static class FissionReactorStateDebug
	{
		internal const string Tag = "[Kerbalism.FissionReactorDbg]";
		internal static bool Enabled = false;

		internal static void Log(Part part, string phase, string detail = null)
		{
			if (!Enabled || part == null)
				return;

			PartModule process = FindFissionProcess(part);
			if (process == null)
				return;

			string vessel = part.vessel != null ? part.vessel.GetDisplayName() : "?";
			string partTitle = part.partInfo != null ? part.partInfo.title : part.name;
			Lib.Log(Lib.BuildString(
				Tag, " ", phase,
				" | vessel=", vessel,
				" | part=", partTitle,
				" | running=", BridgeModuleFields.GetBool(process, "running").ToString(),
				" | broken=", BridgeModuleFields.GetBool(process, "broken").ToString(),
				" | power%=", BridgeModuleFields.GetFloat(process, "CurrentPowerPercent").ToString("F1"),
				detail != null ? Lib.BuildString(" | ", detail) : string.Empty));
		}

		internal static void LogVessel(Vessel v, string phase, string detail = null)
		{
			if (!Enabled || v == null)
				return;

			Lib.Log(Lib.BuildString(Tag, " ", phase, " | vessel=", v.GetDisplayName(),
				detail != null ? Lib.BuildString(" | ", detail) : string.Empty));
		}

		internal static void LogProtoModule(Vessel v, ProtoPartSnapshot part, ProtoPartModuleSnapshot module, string phase, string detail = null)
		{
			if (!Enabled || module == null)
				return;

			string partTitle = part?.partInfo != null ? part.partInfo.title : "?";
			Lib.Log(Lib.BuildString(
				Tag, " ", phase,
				" | vessel=", v != null ? v.GetDisplayName() : "?",
				" | part=", partTitle,
				" | running=", Lib.Proto.GetBool(module, "running").ToString(),
				" | power%=", Lib.Proto.GetFloat(module, "CurrentPowerPercent").ToString("F1"),
				detail != null ? Lib.BuildString(" | ", detail) : string.Empty));
		}

		private static PartModule FindFissionProcess(Part part)
		{
			foreach (PartModule module in part.Modules)
			{
				if (module.moduleName != "ProcessControllerSystemHeat")
					continue;
				if (BridgeModuleFields.GetString(module, "resource") != "_Nukereactor")
					continue;
				return module;
			}

			return null;
		}
	}
}
