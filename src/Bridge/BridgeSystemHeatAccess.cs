using System.Collections;
using System.Reflection;
using UnityEngine;

namespace KerbalismBridge
{
	/// <summary>SystemHeat field access for Bridge core (no KERBALISM SystemHeat wrapper).</summary>
	internal static class BridgeSystemHeatAccess
	{
		public static bool IsModuleSystemHeat(PartModule module) =>
			module != null && module.moduleName == "ModuleSystemHeat";

		public static string GetModuleId(PartModule module) =>
			BridgeModuleFields.GetString(module, "moduleID");

		public static float CurrentLoopTemperature(PartModule heatModule, float fallback = 4f) =>
			BridgeModuleFields.GetFloat(heatModule, "currentLoopTemperature", fallback);

		public static float Get(PartModule module, string name, float fallback) =>
			BridgeModuleFields.GetFloat(module, name, fallback);

		public static void Set(PartModule module, string name, float value)
		{
			if (module == null)
				return;

			FieldInfo field = module.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(float))
				field.SetValue(module, value);
		}

		public static float EvaluateFloatCurveField(PartModule module, string fieldName, float input, float fallback = 0f) =>
			BridgeModuleFields.EvaluateFloatCurve(BridgeModuleFields.GetField<FloatCurve>(module, fieldName), input, fallback);

		public static IList GetResHandlerInputResources(PartModule module)
		{
			if (module == null)
				return null;

			object resHandler = BridgeModuleFields.GetField<object>(module, "resHandler");
			if (resHandler == null)
				return null;

			FieldInfo field = resHandler.GetType().GetField("inputResources", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			return field?.GetValue(resHandler) as IList;
		}
	}
}
