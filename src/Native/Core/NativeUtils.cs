using System;
using System.Reflection;
using KERBALISM.Planner;
using UnityEngine;

namespace KerbalismNative
{
	public static class NativeUtils
	{
		private static DateTime lastPlannerUIUpdate = DateTime.UtcNow;
		private const double PlannerUIUpdateDelayMs = 500.0;

		public static void Log(string msg)
		{
			Debug.Log("[KerbalismNative] " + msg);
		}

		public static void LogWarning(string msg)
		{
			Debug.LogWarning("[KerbalismNative] " + msg);
		}

		public static void LogError(string msg)
		{
			Debug.LogError("[KerbalismNative] " + msg);
		}

		public static void UpdateKerbalismPlannerUI()
		{
			DateTime timeStamp = DateTime.UtcNow;
			if ((timeStamp - lastPlannerUIUpdate).TotalMilliseconds < PlannerUIUpdateDelayMs)
				return;

			lastPlannerUIUpdate = timeStamp;
			string className = typeof(Planner).AssemblyQualifiedName;
			ReflectionStaticCall(className, "RefreshPlanner");
		}

		private static void ReflectionStaticCall(string className, string methodName)
		{
			Type staticClass = Type.GetType(className);
			if (staticClass == null)
				return;

			try
			{
				staticClass.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
			}
			catch (Exception ex)
			{
				LogError("Static class method " + className + "." + methodName + " reflection call failed. Exception: " + ex.Message);
			}
		}

		public static ProtoPartModuleSnapshot FindPartModuleSnapshot(ProtoPartSnapshot partSnapshot, string moduleName)
		{
			return TryFindPartModuleSnapshot(partSnapshot, moduleName);
		}

		public static ProtoPartModuleSnapshot TryFindPartModuleSnapshot(ProtoPartSnapshot partSnapshot, string moduleName)
		{
			if (partSnapshot == null)
				return null;

			for (int i = 0; i < partSnapshot.modules.Count; i++)
			{
				if (partSnapshot.modules[i].moduleName == moduleName)
					return partSnapshot.modules[i];
			}

			return null;
		}
	}
}
