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

			List<HarvestedResource> resources = harvester.resources;
			if (resources == null || resources.Count == 0)
				return;

			double intakeVolume = ComputeIntakeVolume(harvester);
			if (intakeVolume <= double.Epsilon)
				return;

			double altitude = vessel.altitude + vessel.mainBody.Radius;
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

				double sample = SampleSpaceDustResource(name, vessel.mainBody, altitude, vessel.latitude, vessel.longitude);
				double rate = sample * intakeVolume * res.BaseEfficiency * scale / density;
				if (rate <= res.MinHarvestValue)
					continue;

				resourceChangeRequest.Add(new KeyValuePair<string, double>(name, rate));
			}
		}

		private static double ComputeIntakeVolume(ModuleSpaceDustHarvester harvester)
		{
			Vessel vessel = harvester.vessel;
			if (vessel == null)
				return 0d;

			Transform intakeTransform = null;
			if (!string.IsNullOrEmpty(harvester.HarvestIntakeTransformName))
				intakeTransform = harvester.part.FindModelTransform(harvester.HarvestIntakeTransformName);
			if (intakeTransform == null)
				intakeTransform = harvester.part.transform;

			if (harvester.HarvestType == HarvestType.Atmosphere)
			{
				if (vessel.atmDensity <= 0d)
					return 0d;

				Vector3d worldVelocity = vessel.srf_velocity;
				double mach = vessel.mach;
				double dot = Vector3d.Dot(worldVelocity, intakeTransform.forward);
				return (worldVelocity.magnitude * Math.Max(dot, 0d) * harvester.IntakeVelocityScale.Evaluate((float)mach) + harvester.IntakeSpeedStatic) * harvester.IntakeArea;
			}

			if (harvester.HarvestType == HarvestType.Exosphere)
			{
				if (vessel.atmDensity > 0d)
					return 0d;

				Vector3d worldVelocity = vessel.obt_velocity;
				double dot = Vector3d.Dot(worldVelocity.normalized, intakeTransform.forward.normalized);
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
			// SpaceDust harvesting depends on loaded-vessel intake conditions. Once unloaded,
			// force the native harvester off so neither Kerbalism nor SpaceDust background
			// simulation consumes EC, emits heat, or produces resources.
			ProtoPartModuleSnapshot harvesterSnapshot = BridgeUtils.TryFindPartModuleSnapshot(part_snapshot, "ModuleSpaceDustHarvester");
			if (harvesterSnapshot != null && Lib.Proto.GetBool(harvesterSnapshot, "Enabled"))
				Lib.Proto.Set(harvesterSnapshot, "Enabled", false);

			SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
			return brokerTitle;
		}
	}
}
