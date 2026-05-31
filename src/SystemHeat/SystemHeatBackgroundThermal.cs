using System;
using System.Collections.Generic;
using System.Reflection;
using KERBALISM;
using SystemHeat;
using UnityEngine;

namespace KerbalismSystemHeat
{
	/// <summary>
	/// Minimal offline thermal simulation for SystemHeat loops on unloaded vessels.
	/// </summary>
	public static class SystemHeatBackgroundThermal
	{
		private static readonly Dictionary<Guid, double> lastRunTime = new Dictionary<Guid, double>();

		private static readonly string[] FusionReactorModuleNames = { "FusionReactor", "ModuleFusionEngine" };

		internal static bool Enabled = true;
		internal static float RadiatorCoefficient = 0.05f;

		public static void TryRun(Vessel v, double elapsed_s)
		{
			if (!Enabled || v == null || elapsed_s <= 0.0 || v.loaded)
				return;

			double now = Planetarium.GetUniversalTime();
			if (lastRunTime.TryGetValue(v.id, out double last) && last == now)
				return;
			lastRunTime[v.id] = now;

			SimulateVessel(v, (float)elapsed_s);
		}

		private class LoopState
		{
			internal float volume;
			internal float temperature;
			internal float netFluxKw;
			internal float shutdownTemperature = float.MaxValue;
			internal readonly List<ProtoPartModuleSnapshot> heatModules = new List<ProtoPartModuleSnapshot>();
			internal readonly List<(ProtoPartModuleSnapshot module, float shutdown)> heatProducers = new List<(ProtoPartModuleSnapshot, float)>();
		}

		private static void SimulateVessel(Vessel v, float elapsed_s)
		{
			var loops = new Dictionary<int, LoopState>();

			foreach (ProtoPartSnapshot part in v.protoVessel.protoPartSnapshots)
			{
				Part prefab = PartLoader.getPartInfoByName(part.partName).partPrefab;

				foreach (ProtoPartModuleSnapshot module in part.modules)
				{
					if (module.moduleName == "ModuleSystemHeat")
					{
						int loopId = Lib.Proto.GetInt(module, "currentLoopID");
						float loopTemp = Lib.Proto.GetFloat(module, "currentLoopTemperature");
						float volume = GetModuleVolume(prefab, module);

						if (!loops.TryGetValue(loopId, out LoopState loop))
						{
							loop = new LoopState { temperature = loopTemp > 0f ? loopTemp : GetEnvironmentTemperature(v) };
							loops[loopId] = loop;
						}

						loop.volume += volume;
						if (loopTemp > 0f)
							loop.temperature = loopTemp;
						loop.heatModules.Add(module);
					}
					else if (module.moduleName == "ProcessControllerSystemHeat")
					{
						if (!Lib.Proto.GetBool(module, "running") || Lib.Proto.GetBool(module, "broken"))
							continue;

						float power = GetProcessHeatPower(prefab, module);
						int loopId = GetLinkedLoopId(part, prefab, Lib.Proto.GetString(module, "systemHeatModuleID"));
						if (loopId < 0)
							continue;

						EnsureLoop(loops, loopId, v);
						loops[loopId].netFluxKw += power;
						loops[loopId].shutdownTemperature = Math.Min(loops[loopId].shutdownTemperature, Lib.Proto.GetFloat(module, "shutdownTemperature"));
						loops[loopId].heatProducers.Add((module, Lib.Proto.GetFloat(module, "shutdownTemperature")));
					}
					else if (module.moduleName == "HarvesterSystemHeat")
					{
						if (!Lib.Proto.GetBool(module, "deployed") || !Lib.Proto.GetBool(module, "running") || Lib.Proto.GetString(module, "issue").Length > 0)
							continue;

						float power = GetHarvesterHeatPower(prefab, module);
						int loopId = GetLinkedLoopId(part, prefab, Lib.Proto.GetString(module, "systemHeatModuleID"));
						if (loopId < 0)
							continue;

						EnsureLoop(loops, loopId, v);
						loops[loopId].netFluxKw += power;
						loops[loopId].shutdownTemperature = Math.Min(loops[loopId].shutdownTemperature, Lib.Proto.GetFloat(module, "shutdownTemperature"));
						loops[loopId].heatProducers.Add((module, Lib.Proto.GetFloat(module, "shutdownTemperature")));
					}
					else if (module.moduleName == "SystemHeatRadiatorKerbalism")
					{
						if (!Lib.Proto.GetBool(module, "IsCooling"))
							continue;

						int loopId = GetRadiatorLoopId(part, prefab);
						if (loopId < 0)
							continue;

						EnsureLoop(loops, loopId, v);
						float scale = Lib.Proto.GetFloat(module, "scale");
						if (scale <= 0f)
							scale = 1f;
						loops[loopId].netFluxKw -= GetRadiatorRejectPower(prefab, module) * scale;
					}
					else if (module.moduleName == "SystemHeatFissionReactorKerbalismUpdater")
					{
						ProtoPartModuleSnapshot reactor = KSHUtils.FindPartModuleSnapshot(part, "ModuleSystemHeatFissionReactor");
						if (reactor == null || !Lib.Proto.GetBool(reactor, "Enabled"))
							continue;

						ModuleSystemHeatFissionReactor reactorPrefab = prefab.FindModuleImplementing<ModuleSystemHeatFissionReactor>();
						string heatModuleId = reactorPrefab != null ? reactorPrefab.systemHeatModuleID : "reactor";
						int loopId = GetLinkedLoopId(part, prefab, heatModuleId);
						if (loopId < 0)
							continue;

						EnsureLoop(loops, loopId, v);
						float throttle = Lib.Proto.GetFloat(reactor, "CurrentReactorThrottle");
						float heat = GetReactorWasteHeat(reactorPrefab, throttle);
						loops[loopId].netFluxKw += heat;
					}
					else if (module.moduleName == "FFTFusionReactorKerbalismUpdater" || module.moduleName == "FFTFusionEngineKerbalismUpdater")
					{
						string fftReactorModule = module.moduleName == "FFTFusionEngineKerbalismUpdater"
							? "ModuleFusionEngine"
							: "FusionReactor";
						ProtoPartModuleSnapshot reactor = KSHUtils.FindPartModuleSnapshot(part, fftReactorModule);
						if (reactor == null || !Lib.Proto.GetBool(reactor, "Enabled"))
							continue;

						if (!TryGetFusionReactorHeatConfig(prefab, out string heatModuleId, out float systemPower))
							continue;

						int loopId = GetLinkedLoopId(part, prefab, heatModuleId);
						if (loopId < 0)
							continue;

						EnsureLoop(loops, loopId, v);
						loops[loopId].netFluxKw += systemPower;
					}
				}
			}

			float envTemp = GetEnvironmentTemperature(v);
			var coolant = SystemHeatSettings.GetCoolantType("");

			foreach (KeyValuePair<int, LoopState> entry in loops)
			{
				LoopState loop = entry.Value;
				if (loop.volume <= 0f)
					loop.volume = 1f;

				float thermalMass = (float)(loop.volume * coolant.Density * coolant.HeatCapacity);
				if (thermalMass <= 0f)
					continue;

				float deltaT = loop.netFluxKw * 1000f / thermalMass * elapsed_s;
				loop.temperature = Mathf.Clamp(loop.temperature + deltaT, envTemp, 5000f);

				if (loop.netFluxKw <= 0f && loop.temperature > envTemp)
				{
					float decay = (loop.temperature - envTemp) * SystemHeatSettings.HeatLoopDecayCoefficient;
					loop.temperature -= decay * 1000f / thermalMass * elapsed_s;
					loop.temperature = Mathf.Max(loop.temperature, envTemp);
				}

				foreach (ProtoPartModuleSnapshot heatModule in loop.heatModules)
				{
					Lib.Proto.Set(heatModule, "currentLoopTemperature", loop.temperature);
					Lib.Proto.Set(heatModule, "currentLoopFlux", loop.netFluxKw);
				}

				if (loop.temperature >= loop.shutdownTemperature)
				{
					foreach ((ProtoPartModuleSnapshot module, float shutdown) in loop.heatProducers)
					{
						if (loop.temperature >= shutdown)
							Lib.Proto.Set(module, "running", false);
					}
				}
			}
		}

		private static void EnsureLoop(Dictionary<int, LoopState> loops, int loopId, Vessel v)
		{
			if (!loops.ContainsKey(loopId))
				loops[loopId] = new LoopState { temperature = GetEnvironmentTemperature(v) };
		}

		private static float GetEnvironmentTemperature(Vessel v)
		{
			if (v.mainBody != null && v.altitude < 50000d)
				return Mathf.Clamp((float)v.mainBody.GetTemperature(v.altitude), SystemHeatSettings.SpaceTemperature, 50000f);
			return SystemHeatSettings.SpaceTemperature;
		}

		private static float GetModuleVolume(Part prefab, ProtoPartModuleSnapshot module)
		{
			ModuleSystemHeat heat = prefab.FindModuleImplementing<ModuleSystemHeat>();
			if (heat != null)
				return heat.volume;
			return 1f;
		}

		private static float GetProcessHeatPower(Part prefab, ProtoPartModuleSnapshot module)
		{
			foreach (PartModule pm in prefab.Modules)
			{
				if (pm is ProcessControllerSystemHeat pcs && pcs.resource == Lib.Proto.GetString(module, "resource"))
					return pcs.systemPower;
			}
			return Lib.Proto.GetFloat(module, "systemPower");
		}

		private static float GetHarvesterHeatPower(Part prefab, ProtoPartModuleSnapshot module)
		{
			foreach (PartModule pm in prefab.Modules)
			{
				if (pm is HarvesterSystemHeat hs && hs.resource == Lib.Proto.GetString(module, "resource"))
					return hs.systemPower;
			}
			return Lib.Proto.GetFloat(module, "systemPower");
		}

		private static float GetRadiatorRejectPower(Part prefab, ProtoPartModuleSnapshot module)
		{
			ModuleSystemHeatRadiator radiator = prefab.FindModuleImplementing<ModuleSystemHeatRadiator>();
			if (radiator == null)
				return 10f;

			float power = 0f;
			foreach (ModuleResource res in radiator.resHandler.inputResources)
				power += (float)res.rate;
			return power > 0f ? power * SystemHeatBackgroundThermal.RadiatorCoefficient : 10f * SystemHeatBackgroundThermal.RadiatorCoefficient;
		}

		private static float GetReactorWasteHeat(ModuleSystemHeatFissionReactor reactorPrefab, float throttlePercent)
		{
			if (reactorPrefab == null)
				return 0f;

			float heat = (float)reactorPrefab.HeatGeneration.Evaluate(throttlePercent);
			float elec = (float)reactorPrefab.ElectricalGeneration.Evaluate(throttlePercent);
			return Math.Max(0f, heat - elec);
		}

		private static int GetLinkedLoopId(ProtoPartSnapshot part, Part prefab, string moduleId)
		{
			foreach (ModuleSystemHeat heat in prefab.FindModulesImplementing<ModuleSystemHeat>())
			{
				if (string.IsNullOrEmpty(moduleId) || heat.moduleID == moduleId)
				{
					ProtoPartModuleSnapshot heatModule = KSHUtils.FindPartModuleSnapshot(part, "ModuleSystemHeat");
					if (heatModule != null)
						return Lib.Proto.GetInt(heatModule, "currentLoopID");
				}
			}
			return -1;
		}

		private static int GetRadiatorLoopId(ProtoPartSnapshot part, Part prefab)
		{
			if (prefab.FindModuleImplementing<ModuleSystemHeat>() == null)
				return -1;

			ProtoPartModuleSnapshot heatModule = KSHUtils.FindPartModuleSnapshot(part, "ModuleSystemHeat");
			return heatModule != null ? Lib.Proto.GetInt(heatModule, "currentLoopID") : -1;
		}

		private static bool TryGetFusionReactorHeatConfig(Part prefab, out string heatModuleId, out float systemPower)
		{
			heatModuleId = "";
			systemPower = 0f;

			foreach (string moduleName in FusionReactorModuleNames)
			{
				PartModule module = FindPrefabModule(prefab, moduleName);
				if (module == null)
					continue;

				Type type = module.GetType();
				heatModuleId = ReadField<string>(module, type, "HeatModuleID") ?? "";
				systemPower = ReadField<float>(module, type, "SystemPower");
				return systemPower > 0f;
			}
			return false;
		}

		private static PartModule FindPrefabModule(Part prefab, string moduleName)
		{
			foreach (PartModule module in prefab.Modules)
			{
				if (module.moduleName == moduleName)
					return module;
			}
			return null;
		}

		private static T ReadField<T>(PartModule module, Type type, string fieldName)
		{
			FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
				return default;
			object value = field.GetValue(module);
			return value is T typed ? typed : default;
		}
	}
}
