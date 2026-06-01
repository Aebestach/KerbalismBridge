using System.Reflection;
using UnityEngine;

namespace KerbalismBridge
{
	public static class BridgeModuleFields
	{
		public static bool GetBool(PartModule module, string name, bool fallback = false)
		{
			if (module == null)
				return fallback;

			FieldInfo field = module.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(bool))
				return (bool)field.GetValue(module);

			return fallback;
		}

		public static float GetFloat(PartModule module, string name, float fallback = 0f)
		{
			if (module == null)
				return fallback;

			FieldInfo field = module.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(float))
				return (float)field.GetValue(module);

			return fallback;
		}

		public static string GetString(PartModule module, string name, string fallback = "")
		{
			if (module == null)
				return fallback;

			FieldInfo field = module.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(string))
				return (string)field.GetValue(module) ?? fallback;

			return fallback;
		}

		public static T GetField<T>(PartModule module, string name, T fallback = default)
		{
			if (module == null)
				return fallback;

			FieldInfo field = module.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(T))
				return (T)field.GetValue(module);

			return fallback;
		}
	}
}
