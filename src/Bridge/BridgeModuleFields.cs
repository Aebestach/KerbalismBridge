using System;
using System.Collections.Generic;
using System.Reflection;

namespace KerbalismBridge
{
	public static class BridgeModuleFields
	{
		private const BindingFlags InstanceFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private static readonly object FieldCacheLock = new object();
		private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> FieldCache =
			new Dictionary<Type, Dictionary<string, FieldInfo>>();

		public static bool GetBool(PartModule module, string name, bool fallback = false)
		{
			if (module == null)
				return fallback;

			FieldInfo field = GetField(module.GetType(), name);
			if (field != null && field.FieldType == typeof(bool))
				return (bool)field.GetValue(module);

			return fallback;
		}

		public static float GetFloat(PartModule module, string name, float fallback = 0f)
		{
			if (module == null)
				return fallback;

			FieldInfo field = GetField(module.GetType(), name);
			if (field != null && field.FieldType == typeof(float))
				return (float)field.GetValue(module);

			return fallback;
		}

		public static string GetString(PartModule module, string name, string fallback = "")
		{
			if (module == null)
				return fallback;

			FieldInfo field = GetField(module.GetType(), name);
			if (field != null && field.FieldType == typeof(string))
				return (string)field.GetValue(module) ?? fallback;

			return fallback;
		}

		public static T GetField<T>(PartModule module, string name, T fallback = default)
		{
			if (module == null)
				return fallback;

			FieldInfo field = GetField(module.GetType(), name);
			if (field != null && typeof(T).IsAssignableFrom(field.FieldType))
				return (T)field.GetValue(module);

			return fallback;
		}

		private static FieldInfo GetField(Type type, string name)
		{
			Dictionary<string, FieldInfo> fields;
			lock (FieldCacheLock)
			{
				if (!FieldCache.TryGetValue(type, out fields))
				{
					fields = new Dictionary<string, FieldInfo>();
					FieldCache[type] = fields;
				}

				FieldInfo field;
				if (!fields.TryGetValue(name, out field))
				{
					field = type.GetField(name, InstanceFieldFlags);
					fields[name] = field;
				}

				return field;
			}
		}
	}
}
