using System;
using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;
using UnityEngine;
using KerbalismBridge;
using SpaceDust;

namespace KerbalismSpaceDust
{
	/// <summary>
	/// Kerbalism resource routing for SpaceDust harvesters; native module keeps intake physics, heat, and UI.
	/// </summary>
	public class SpaceDustHarvesterKerbalismUpdater : PartModule, IKerbalismModule
	{
		public static string brokerName = "SpaceDustHarvester";
		public static string brokerTitle = Localizer.Format("#LOC_SpaceDust_ModuleSpaceDustHarvester_DisplayName");

		[KSPField(isPersistant = true)]
		public string harvesterModuleID = "harvester";

		private ModuleSpaceDustHarvester nativeHarvester;
		private bool nativeResolved;

		private ModuleSpaceDustHarvester NativeHarvester
		{
			get
			{
				if (!nativeResolved)
				{
					nativeResolved = true;
					nativeHarvester = part.FindModuleImplementing<ModuleSpaceDustHarvester>();
				}

				return nativeHarvester;
			}
		}

		private bool IsEnabled()
		{
			ModuleSpaceDustHarvester harvester = NativeHarvester;
			return harvester != null && harvester.Enabled;
		}

		private float GetPowerCost()
		{
			ModuleSpaceDustHarvester harvester = NativeHarvester;
			return harvester != null ? harvester.PowerCost : 0f;
		}

		private double GetThermalScale()
		{
			ModuleSpaceDustHarvester harvester = NativeHarvester;
			if (harvester == null)
				return 1d;

			PartModule heatModule = FindLinkedHeatModule(harvester);
			if (heatModule == null)
				return 1d;

			float loopTemp = BridgeModuleFields.GetFloat(heatModule, "currentLoopTemperature");
			return harvester.SystemEfficiency.Evaluate(loopTemp);
		}

		private PartModule FindLinkedHeatModule(ModuleSpaceDustHarvester harvester)
		{
			foreach (PartModule module in part.Modules)
			{
				if (module.moduleName != "ModuleSystemHeat")
					continue;
				if (BridgeModuleFields.GetString(module, "moduleID") == harvester.HeatModuleID)
					return module;
			}

			return null;
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (!IsEnabled())
				return brokerTitle;

			double scale = GetThermalScale();
			float powerCost = GetPowerCost();
			if (powerCost > 0f)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -powerCost * scale));

			AddHarvestRates(resourceChangeRequest, scale);
			return brokerTitle;
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (!IsEnabled())
				return brokerTitle;

			float powerCost = GetPowerCost();
			if (powerCost > 0f)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -powerCost));

			AddHarvestRates(resourceChangeRequest, 1d);
			return brokerTitle;
		}

		private void AddHarvestRates(List<KeyValuePair<string, double>> resourceChangeRequest, double scale)
		{
			ModuleSpaceDustHarvester harvester = NativeHarvester;
			if (harvester == null || vessel == null)
				return;

			AddHarvestRatesFromModule(harvester, vessel, resourceChangeRequest, scale);
		}

		internal static void AddBackgroundHarvestRates(
			Vessel v,
			ModuleSpaceDustHarvester harvesterPrefab,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			ProtoPartSnapshot partSnapshot,
			string harvesterModuleId)
		{
			if (v == null || harvesterPrefab == null || partSnapshot == null)
				return;

			ProtoPartModuleSnapshot harvesterSnapshot = FindHarvesterSnapshot(partSnapshot, harvesterModuleId);
			if (harvesterSnapshot == null || !Lib.Proto.GetBool(harvesterSnapshot, "Enabled"))
				return;

			double scale = GetBackgroundThermalScale(partSnapshot, harvesterPrefab);
			float powerCost = harvesterPrefab.PowerCost;
			if (powerCost > 0f)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -powerCost * scale));

			AddHarvestRatesFromModule(harvesterPrefab, v, resourceChangeRequest, scale);
		}

		private static ProtoPartModuleSnapshot FindHarvesterSnapshot(ProtoPartSnapshot part, string harvesterModuleId)
		{
			ProtoPartModuleSnapshot fallback = null;
			foreach (ProtoPartModuleSnapshot module in part.modules)
			{
				if (module.moduleName != "ModuleSpaceDustHarvester")
					continue;

				if (fallback == null)
					fallback = module;

				string moduleId = Lib.Proto.GetString(module, "ModuleID");
				if (string.IsNullOrEmpty(harvesterModuleId) || moduleId == harvesterModuleId)
					return module;
			}

			return fallback;
		}

		private static double GetBackgroundThermalScale(ProtoPartSnapshot part, ModuleSpaceDustHarvester harvesterPrefab)
		{
			string heatModuleId = harvesterPrefab.HeatModuleID;
			if (string.IsNullOrEmpty(heatModuleId))
				return 1d;

			foreach (ProtoPartModuleSnapshot module in part.modules)
			{
				if (module.moduleName != "ModuleSystemHeat")
					continue;
				if (Lib.Proto.GetString(module, "moduleID") != heatModuleId)
					continue;

				float loopTemp = Lib.Proto.GetFloat(module, "currentLoopTemperature");
				if (loopTemp <= 0f)
					return 1d;

				return harvesterPrefab.SystemEfficiency.Evaluate(loopTemp);
			}

			return 1d;
		}

		private static void AddHarvestRatesFromModule(
			ModuleSpaceDustHarvester harvester,
			Vessel v,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			double scale)
		{
			List<HarvestedResource> resources = harvester.resources;
			if (resources == null || resources.Count == 0)
				return;

			double intakeVolume = ComputeIntakeVolume(harvester, v);
			if (intakeVolume <= double.Epsilon)
				return;

			double altitude = v.altitude + v.mainBody.Radius;
			foreach (HarvestedResource res in resources)
			{
				if (res == null)
					continue;

				string name = res.Name;
				if (string.IsNullOrEmpty(name))
					continue;

				double density = res.density;
				if (density <= double.Epsilon)
					density = 0.05d;

				double sample = SampleSpaceDustResource(name, v.mainBody, altitude, v.latitude, v.longitude);
				double rate = sample * intakeVolume * res.BaseEfficiency * scale / density;
				if (rate <= res.MinHarvestValue)
					continue;

				resourceChangeRequest.Add(new KeyValuePair<string, double>(name, rate));
			}
		}

		private static double ComputeIntakeVolume(ModuleSpaceDustHarvester harvester, Vessel v)
		{
			if (harvester == null || v == null || v.mainBody == null)
				return 0d;

			Transform intakeTransform = null;
			if (harvester.part != null)
			{
				if (!string.IsNullOrEmpty(harvester.HarvestIntakeTransformName))
					intakeTransform = harvester.part.FindModelTransform(harvester.HarvestIntakeTransformName);
				if (intakeTransform == null)
					intakeTransform = harvester.part.transform;
			}

			if (harvester.HarvestType == HarvestType.Atmosphere)
			{
				if (v.atmDensity <= 0d)
					return 0d;

				Vector3d worldVelocity = v.srf_velocity;
				double mach = v.mach;
				double dot = intakeTransform != null
					? Vector3d.Dot(worldVelocity, intakeTransform.forward)
					: worldVelocity.magnitude;
				return (worldVelocity.magnitude * Math.Max(dot, 0d) * harvester.IntakeVelocityScale.Evaluate((float)mach) + harvester.IntakeSpeedStatic) * harvester.IntakeArea;
			}

			if (harvester.HarvestType == HarvestType.Exosphere)
			{
				if (v.atmDensity > 0d)
					return 0d;

				Vector3d worldVelocity = v.obt_velocity;
				double dot = intakeTransform != null
					? Vector3d.Dot(worldVelocity.normalized, intakeTransform.forward.normalized)
					: 1d;
				return (worldVelocity.magnitude * Math.Max(dot, 0d) + harvester.IntakeSpeedStatic) * harvester.IntakeArea;
			}

			return harvester.IntakeSpeedStatic * harvester.IntakeArea;
		}

		private static double SampleSpaceDustResource(string resourceName, CelestialBody body, double altitude, double latitude, double longitude)
		{
			SpaceDustResourceMap map = SpaceDustResourceMap.Instance;
			if (map == null)
				return 0d;

			return map.SampleResource(resourceName, body, altitude, latitude, longitude);
		}

		public static string BackgroundUpdate(
			Vessel v,
			ProtoPartSnapshot part_snapshot,
			ProtoPartModuleSnapshot module_snapshot,
			PartModule proto_part_module,
			Part proto_part,
			Dictionary<string, double> availableResources,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			double elapsed_s)
		{
			string harvesterModuleId = Lib.Proto.GetString(module_snapshot, "harvesterModuleID", "harvester");
			ModuleSpaceDustHarvester harvesterPrefab = proto_part_module as ModuleSpaceDustHarvester;
			if (harvesterPrefab == null && proto_part != null)
				harvesterPrefab = proto_part.FindModuleImplementing<ModuleSpaceDustHarvester>();

			AddBackgroundHarvestRates(v, harvesterPrefab, resourceChangeRequest, part_snapshot, harvesterModuleId);
			SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
			return brokerTitle;
		}
	}
}
