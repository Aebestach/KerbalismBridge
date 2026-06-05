using System;
using System.Reflection;

namespace KerbalismCryo
{
	internal static class SystemHeatBackgroundBridge
	{
		private static MethodInfo tryRunMethod;
		private static bool lookupDone;

		internal static void TryRun(Vessel v, double elapsed_s)
		{
			if (v == null || elapsed_s <= 0.0)
				return;

			MethodInfo method = GetTryRunMethod();
			if (method == null)
				return;

			try
			{
				method.Invoke(null, new object[] { v, elapsed_s });
			}
			catch (Exception ex)
			{
				CryoUtils.LogError("SystemHeatBackgroundThermal.TryRun failed: " + ex.Message);
			}
		}

		private static MethodInfo GetTryRunMethod()
		{
			if (lookupDone)
				return tryRunMethod;

			lookupDone = true;
			foreach (AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies)
			{
				if (loaded.assembly.GetName().Name != "zKerbalismBridge")
					continue;

				Type type = loaded.assembly.GetType("KerbalismBridge.SystemHeatBackgroundThermal");
				if (type == null)
					break;

				tryRunMethod = type.GetMethod(
					"TryRun",
					BindingFlags.Static | BindingFlags.Public,
					null,
					new[] { typeof(Vessel), typeof(double) },
					null);
				break;
			}

			return tryRunMethod;
		}
	}
}
