using System.Collections.Generic;
using HarmonyLib;
using KERBALISM;
using KERBALISM.Planner;

namespace KerbalismProcess
{
	/// <summary>
	/// Kerbalism Planner uses pseudo-resource flow for ProcessController rates.
	/// Sync _Nukereactor before each simulator pass so VAB/SPH EC matches running Layer A reactors.
	/// </summary>
	[HarmonyPatch(typeof(ResourceSimulator), "RunSimulator")]
	internal static class Patch_ResourceSimulator_RunSimulator_PlannerSync
	{
		private static void Prefix(List<Part> parts)
		{
			if (!Lib.IsEditor() || parts == null)
				return;

			foreach (Part part in parts)
			{
				foreach (PartModule module in part.Modules)
				{
					if (module is ProcessControllerSystemHeat heat && heat.resource == "_Nukereactor")
						heat.SyncPlannerPseudoResource();
				}
			}
		}
	}
}
