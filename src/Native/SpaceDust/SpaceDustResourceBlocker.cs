using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using KSP.Localization;
using KERBALISM;
using UnityEngine;

namespace KerbalismNative
{
	/// <summary>
	/// While native SpaceDust harvesters run logic/UI, block their direct Part.RequestResource calls
	/// so Kerbalism owns resource accounting on parts with SpaceDustHarvesterKerbalismUpdater.
	/// </summary>
	internal static class SpaceDustResourceBlocker
	{
		[ThreadStatic]
		private static int blockDepth;

		internal static bool IsBlocking => blockDepth > 0;

		internal static void EnterBlock() => blockDepth++;

		internal static void ExitBlock()
		{
			if (blockDepth > 0)
				blockDepth--;
		}

		internal static bool ShouldBlockRequest(Part part)
		{
			if (!IsBlocking || part == null)
				return false;

			return part.FindModuleImplementing<SpaceDustHarvesterKerbalismUpdater>() != null;
		}

		internal static bool IsSpaceDustHarvesterFrame()
		{
			var trace = new StackTrace(false);
			for (int i = 0; i < trace.FrameCount && i < 12; i++)
			{
				MethodBase method = trace.GetFrame(i)?.GetMethod();
				if (method == null)
					continue;

				Type declaring = method.DeclaringType;
				if (declaring == null)
					continue;

				if (declaring.FullName == "SpaceDust.ModuleSpaceDustHarvester")
					return true;
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(Part), "RequestResource", new[] { typeof(int), typeof(double), typeof(ResourceFlowMode), typeof(bool) })]
	internal static class Patch_Part_RequestResource_SpaceDust
	{
		private static bool Prefix(Part __instance, double requestAmount, ref double __result)
		{
			if (!SpaceDustResourceBlocker.ShouldBlockRequest(__instance)
				|| !SpaceDustResourceBlocker.IsSpaceDustHarvesterFrame())
				return true;

			__result = requestAmount;
			return false;
		}
	}
}
