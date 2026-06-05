using System;
using System.Collections.Generic;
using System.Reflection;
using KERBALISM;

namespace KerbalismDynamicRadiation
{
	/// <summary>
	/// Shared flight / background radiation scaling for fission and fusion power sources.
	/// </summary>
	static class DynamicRadiationLogic
	{
		private const BindingFlags PublicInstanceMembers = BindingFlags.Instance | BindingFlags.Public;
		private static readonly object ReflectionCacheLock = new object();
		private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> FieldCache =
			new Dictionary<Type, Dictionary<string, FieldInfo>>();
		private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache =
			new Dictionary<Type, Dictionary<string, PropertyInfo>>();
		private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> MethodCache =
			new Dictionary<Type, Dictionary<string, MethodInfo>>();

		public static void UpdateFlight(
			Emitter emitter,
			bool powerEnabled,
			ref bool reactorHasStarted,
			ref double reactorStoppedAt,
			double emitterMaxRadiation,
			double minEmissionPercent,
			double emissionDecayRate)
		{
			if (emitter == null || emitterMaxRadiation <= 0.0)
				return;

			double minRadiation = emitterMaxRadiation * minEmissionPercent / 100.0;
			double now = Planetarium.GetUniversalTime();

			if (powerEnabled)
			{
				reactorHasStarted = true;
				reactorStoppedAt = 0.0;
				emitter.running = true;
				emitter.radiation = emitterMaxRadiation;
				return;
			}

			if (!reactorHasStarted)
			{
				emitter.running = false;
				emitter.radiation = minRadiation;
				return;
			}

			if (reactorStoppedAt <= 0.0)
				reactorStoppedAt = now;

			double elapsed = now - reactorStoppedAt;
			double decayed = minRadiation + (emitterMaxRadiation - minRadiation) * Math.Exp(-elapsed / emissionDecayRate);
			emitter.radiation = decayed;
			emitter.running = decayed > minRadiation * 1.001;
		}

		public static void UpdateBackground(
			ProtoPartModuleSnapshot emitterSnapshot,
			bool powerEnabled,
			ref bool reactorHasStarted,
			ref double reactorStoppedAt,
			double emitterMaxRadiation,
			double minEmissionPercent,
			double emissionDecayRate,
			double elapsed_s)
		{
			if (emitterSnapshot == null || emitterMaxRadiation <= 0.0)
				return;

			double minRadiation = emitterMaxRadiation * minEmissionPercent / 100.0;

			if (powerEnabled)
			{
				reactorHasStarted = true;
				reactorStoppedAt = 0.0;
				Lib.Proto.Set(emitterSnapshot, "running", true);
				Lib.Proto.Set(emitterSnapshot, "radiation", emitterMaxRadiation);
				return;
			}

			if (!reactorHasStarted)
			{
				Lib.Proto.Set(emitterSnapshot, "running", false);
				Lib.Proto.Set(emitterSnapshot, "radiation", minRadiation);
				return;
			}

			double stoppedAt = reactorStoppedAt;
			if (stoppedAt <= 0.0)
			{
				stoppedAt = Planetarium.GetUniversalTime();
				reactorStoppedAt = stoppedAt;
			}

			double current = Lib.Proto.GetDouble(emitterSnapshot, "radiation");
			if (current <= 0.0)
				current = emitterMaxRadiation;

			// Step decay using background elapsed time (can be large under timewarp).
			double target = minRadiation + (current - minRadiation) * Math.Exp(-elapsed_s / emissionDecayRate);
			if (target < minRadiation)
				target = minRadiation;

			Lib.Proto.Set(emitterSnapshot, "radiation", target);
			Lib.Proto.Set(emitterSnapshot, "running", target > minRadiation * 1.001);
		}

		public static double ResolvePeakRadiation(Part part, Emitter emitter, double minEmissionPercent, double persistedPeak)
		{
			if (persistedPeak > 0.0)
				return persistedPeak;

			double prefabPeak = FindPeakEmitterRadiation(part.partInfo?.partPrefab);
			if (prefabPeak > 0.0)
				return prefabPeak;

			if (emitter != null && emitter.radiation > 0.0)
			{
				if (minEmissionPercent > 0.0 && minEmissionPercent < 100.0)
				{
					double inferred = emitter.radiation * 100.0 / minEmissionPercent;
					if (inferred > emitter.radiation * 1.01)
						return inferred;
				}

				return emitter.radiation;
			}

			return 0.0;
		}

		static double FindPeakEmitterRadiation(Part prefab)
		{
			if (prefab == null)
				return 0.0;

			double best = 0.0;
			for (int i = 0; i < prefab.Modules.Count; i++)
			{
				Emitter e = prefab.Modules[i] as Emitter;
				if (e == null || e.radiation <= 0.0)
					continue;

				if (e.radiation > best)
					best = e.radiation;
			}

			return best;
		}

		public static Emitter FindPrimaryEmitter(Part part, ref int emitterIndex)
		{
			Emitter best = null;
			int bestIndex = -1;
			double bestRadiation = 0.0;

			for (int i = 0; i < part.Modules.Count; i++)
			{
				Emitter e = part.Modules[i] as Emitter;
				if (e == null || e.radiation <= 0.0)
					continue;

				if (e.radiation > bestRadiation)
				{
					bestRadiation = e.radiation;
					best = e;
					bestIndex = i;
				}
			}

			emitterIndex = bestIndex;
			return best;
		}

		public static ProtoPartModuleSnapshot FindEmitterSnapshot(ProtoPartSnapshot protoPart, int emitterIndex, double emitterMaxRadiation)
		{
			if (protoPart == null)
				return null;

			ProtoPartModuleSnapshot byIndex = null;
			ProtoPartModuleSnapshot best = null;
			double bestRadiation = 0.0;

			for (int i = 0; i < protoPart.modules.Count; i++)
			{
				ProtoPartModuleSnapshot pm = protoPart.modules[i];
				if (pm.moduleName != "Emitter")
					continue;

				if (i == emitterIndex)
					byIndex = pm;

				double rad = Lib.Proto.GetDouble(pm, "radiation");
				if (rad > bestRadiation)
				{
					bestRadiation = rad;
					best = pm;
				}
			}

			if (byIndex != null)
				return byIndex;

			if (best != null)
				return best;

			// Fallback: match configured peak radiation.
			for (int i = 0; i < protoPart.modules.Count; i++)
			{
				ProtoPartModuleSnapshot pm = protoPart.modules[i];
				if (pm.moduleName != "Emitter")
					continue;

				if (Math.Abs(Lib.Proto.GetDouble(pm, "radiation") - emitterMaxRadiation) < emitterMaxRadiation * 0.01)
					return pm;
			}

			return null;
		}

		public static bool GetPowerEnabled(Part part, string powerModuleName, string powerModuleId, string powerActiveMode)
		{
			if (powerActiveMode == "any_running")
				return AnyPowerModuleRunning(part, powerModuleName, powerModuleId);

			PartModule match = FindPowerModule(part, powerModuleName, powerModuleId);
			return IsPowerActive(match, powerActiveMode);
		}

		public static bool GetPowerEnabledProto(ProtoPartSnapshot protoPart, string powerModuleName, string powerModuleId, string powerActiveMode)
		{
			if (powerActiveMode == "any_running")
				return AnyPowerModuleRunningProto(protoPart, powerModuleName, powerModuleId);

			ProtoPartModuleSnapshot match = FindPowerModuleProto(protoPart, powerModuleName, powerModuleId);
			return IsPowerActiveProto(match, powerActiveMode);
		}

		static bool AnyPowerModuleRunning(Part part, string powerModuleName, string powerModuleId)
		{
			if (part == null || string.IsNullOrEmpty(powerModuleName))
				return false;

			for (int i = 0; i < part.Modules.Count; i++)
			{
				PartModule pm = part.Modules[i];
				if (!ModuleNameMatches(pm.moduleName, powerModuleName))
					continue;

				if (!string.IsNullOrEmpty(powerModuleId) && GetModuleId(pm) != powerModuleId)
					continue;

				if (IsPowerActive(pm, "running"))
					return true;
			}

			return false;
		}

		static bool AnyPowerModuleRunningProto(ProtoPartSnapshot protoPart, string powerModuleName, string powerModuleId)
		{
			if (protoPart == null || string.IsNullOrEmpty(powerModuleName))
				return false;

			for (int i = 0; i < protoPart.modules.Count; i++)
			{
				ProtoPartModuleSnapshot pm = protoPart.modules[i];
				if (!ModuleNameMatches(pm.moduleName, powerModuleName))
					continue;

				if (!string.IsNullOrEmpty(powerModuleId))
				{
					string id = Lib.Proto.GetString(pm, "moduleID");
					if (string.IsNullOrEmpty(id))
						id = Lib.Proto.GetString(pm, "ModuleID");
					if (string.IsNullOrEmpty(id))
						id = Lib.Proto.GetString(pm, "resource");
					if (id != powerModuleId)
						continue;
				}

				if (Lib.Proto.GetBool(pm, "running"))
					return true;
			}

			return false;
		}

		static PartModule FindPowerModule(Part part, string powerModuleName, string powerModuleId)
		{
			if (part == null || string.IsNullOrEmpty(powerModuleName))
				return null;

			PartModule match = null;
			PartModule first = null;

			for (int i = 0; i < part.Modules.Count; i++)
			{
				PartModule pm = part.Modules[i];
				if (!ModuleNameMatches(pm.moduleName, powerModuleName))
					continue;

				if (first == null)
					first = pm;

				if (string.IsNullOrEmpty(powerModuleId))
				{
					match = pm;
					break;
				}

				if (GetModuleId(pm) == powerModuleId)
				{
					match = pm;
					break;
				}
			}

			return match ?? first;
		}

		static ProtoPartModuleSnapshot FindPowerModuleProto(ProtoPartSnapshot protoPart, string powerModuleName, string powerModuleId)
		{
			if (protoPart == null || string.IsNullOrEmpty(powerModuleName))
				return null;

			ProtoPartModuleSnapshot match = null;
			ProtoPartModuleSnapshot first = null;

			for (int i = 0; i < protoPart.modules.Count; i++)
			{
				ProtoPartModuleSnapshot pm = protoPart.modules[i];
				if (!ModuleNameMatches(pm.moduleName, powerModuleName))
					continue;

				if (first == null)
					first = pm;

				if (string.IsNullOrEmpty(powerModuleId))
				{
					match = pm;
					break;
				}

				string id = Lib.Proto.GetString(pm, "moduleID");
				if (string.IsNullOrEmpty(id))
					id = Lib.Proto.GetString(pm, "ModuleID");
				if (string.IsNullOrEmpty(id))
					id = Lib.Proto.GetString(pm, "resource");
				if (id == powerModuleId)
				{
					match = pm;
					break;
				}
			}

			return match ?? first;
		}

		static bool ModuleNameMatches(string moduleName, string powerModuleName)
		{
			if (moduleName == powerModuleName)
				return true;

			// Stock / mod rocket engines (ModuleEngines, ModuleEnginesFX, …).
			if (powerModuleName == "ModuleEngines" && moduleName.StartsWith("ModuleEngines"))
				return true;

			return false;
		}

		static bool IsPowerActive(PartModule pm, string powerActiveMode)
		{
			if (pm == null)
				return false;

			if (powerActiveMode == "thrust")
				return IsEngineThrusting(pm);

			if (powerActiveMode == "running")
				return ReadBoolField(pm, "running");

			if (powerActiveMode == "converter")
				return IsResourceConverterActive(pm);

			return GetEnabled(pm);
		}

		static bool IsPowerActiveProto(ProtoPartModuleSnapshot pm, string powerActiveMode)
		{
			if (pm == null)
				return false;

			if (powerActiveMode == "thrust")
			{
				float throttle = Lib.Proto.GetFloat(pm, "throttle");
				bool flameout = Lib.Proto.GetBool(pm, "flameout");
				return throttle > 0.01f && !flameout;
			}

			if (powerActiveMode == "running")
				return Lib.Proto.GetBool(pm, "running");

			if (powerActiveMode == "converter")
			{
				if (Lib.Proto.GetBool(pm, "DisabledByEngineer"))
					return false;
				return Lib.Proto.GetBool(pm, "IsEnabled") || Lib.Proto.GetBool(pm, "Enabled");
			}

			return Lib.Proto.GetBool(pm, "Enabled");
		}

		static bool IsEngineThrusting(PartModule pm)
		{
			if (pm == null)
				return false;

			bool flameout = ReadBoolField(pm, "flameout");
			if (flameout)
				return false;

			float throttle = ReadFloatField(pm, "throttle");
			if (throttle > 0.01f)
				return true;

			float thrust = ReadFloatField(pm, "currentThrust");
			return thrust > 0.01f;
		}

		static bool IsResourceConverterActive(PartModule pm)
		{
			if (pm == null)
				return false;

			if (ReadBoolField(pm, "DisabledByEngineer"))
				return false;

			MethodInfo method = GetCachedMethod(pm.GetType(), "IsActivated");
			if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
				return (bool)method.Invoke(pm, null);

			if (ReadBoolField(pm, "IsEnabled"))
				return true;

			return GetEnabled(pm);
		}

		static bool ReadBoolField(PartModule pm, string name)
		{
			FieldInfo field = GetCachedField(pm.GetType(), name);
			if (field != null && field.FieldType == typeof(bool))
				return (bool)field.GetValue(pm);

			return false;
		}

		static float ReadFloatField(PartModule pm, string name)
		{
			FieldInfo field = GetCachedField(pm.GetType(), name);
			if (field != null && field.FieldType == typeof(float))
				return (float)field.GetValue(pm);

			return 0f;
		}

		static string GetModuleId(PartModule pm)
		{
			if (pm == null)
				return string.Empty;

			string id = ReadStringField(pm, "moduleID");
			if (!string.IsNullOrEmpty(id))
				return id;

			id = ReadStringField(pm, "ModuleID");
			if (!string.IsNullOrEmpty(id))
				return id;

			return ReadStringField(pm, "resource");
		}

		static string ReadStringField(PartModule pm, string name)
		{
			FieldInfo field = GetCachedField(pm.GetType(), name);
			if (field != null && field.FieldType == typeof(string))
				return (string)field.GetValue(pm) ?? string.Empty;

			return string.Empty;
		}

		static bool GetEnabled(PartModule pm)
		{
			if (pm == null)
				return false;

			FieldInfo field = GetCachedField(pm.GetType(), "Enabled");
			if (field != null && field.FieldType == typeof(bool))
				return (bool)field.GetValue(pm);

			PropertyInfo prop = GetCachedProperty(pm.GetType(), "Enabled");
			if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
				return (bool)prop.GetValue(pm, null);

			return false;
		}

		static FieldInfo GetCachedField(Type type, string name)
		{
			Dictionary<string, FieldInfo> fields;
			lock (ReflectionCacheLock)
			{
				if (!FieldCache.TryGetValue(type, out fields))
				{
					fields = new Dictionary<string, FieldInfo>();
					FieldCache[type] = fields;
				}

				FieldInfo field;
				if (!fields.TryGetValue(name, out field))
				{
					field = type.GetField(name, PublicInstanceMembers);
					fields[name] = field;
				}

				return field;
			}
		}

		static PropertyInfo GetCachedProperty(Type type, string name)
		{
			Dictionary<string, PropertyInfo> properties;
			lock (ReflectionCacheLock)
			{
				if (!PropertyCache.TryGetValue(type, out properties))
				{
					properties = new Dictionary<string, PropertyInfo>();
					PropertyCache[type] = properties;
				}

				PropertyInfo property;
				if (!properties.TryGetValue(name, out property))
				{
					property = type.GetProperty(name, PublicInstanceMembers);
					properties[name] = property;
				}

				return property;
			}
		}

		static MethodInfo GetCachedMethod(Type type, string name)
		{
			Dictionary<string, MethodInfo> methods;
			lock (ReflectionCacheLock)
			{
				if (!MethodCache.TryGetValue(type, out methods))
				{
					methods = new Dictionary<string, MethodInfo>();
					MethodCache[type] = methods;
				}

				MethodInfo method;
				if (!methods.TryGetValue(name, out method))
				{
					method = type.GetMethod(name, PublicInstanceMembers);
					methods[name] = method;
				}

				return method;
			}
		}
	}
}
