using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KSP.Localization;
using KERBALISM;
using UnityEngine;
using KerbalismBridge;

namespace KerbalismNative
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

		private PartModule nativeHarvester;
		private bool nativeResolved;

		private PartModule NativeHarvester
		{
			get
			{
				if (!nativeResolved)
				{
					nativeResolved = true;
					foreach (PartModule module in part.Modules)
					{
						if (module.moduleName == "ModuleSpaceDustHarvester")
						{
							nativeHarvester = module;
							break;
						}
					}
				}

				return nativeHarvester;
			}
		}

		private bool IsEnabled()
		{
			return BridgeModuleFields.GetBool(NativeHarvester, "Enabled");
		}

		private float GetPowerCost()
		{
			return BridgeModuleFields.GetFloat(NativeHarvester, "PowerCost");
		}

		private double GetThermalScale()
		{
			PartModule harvester = NativeHarvester;
			if (harvester == null)
				return 1d;

			PartModule heatModule = FindLinkedHeatModule(harvester);
			if (heatModule == null)
				return 1d;

			FloatCurve curve = BridgeModuleFields.GetField(harvester, "SystemEfficiency", new FloatCurve());
			float loopTemp = BridgeModuleFields.GetFloat(heatModule, "currentLoopTemperature");
			return curve.Evaluate(loopTemp);
		}

		private PartModule FindLinkedHeatModule(PartModule harvester)
		{
			string heatId = BridgeModuleFields.GetString(harvester, "HeatModuleID");
			foreach (PartModule module in part.Modules)
			{
				if (module.moduleName != "ModuleSystemHeat")
					continue;
				if (BridgeModuleFields.GetString(module, "moduleID") == heatId)
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
			PartModule harvester = NativeHarvester;
			if (harvester == null || vessel == null)
				return;

			IList resources = BridgeModuleFields.GetField<IList>(harvester, "resources", null);
			if (resources == null || resources.Count == 0)
				return;

			int harvestType = GetHarvestType(harvester);
			double intakeVolume = ComputeIntakeVolume(harvester, harvestType);
			if (intakeVolume <= double.Epsilon)
				return;

			double altitude = vessel.altitude + vessel.mainBody.Radius;
			foreach (object resObj in resources)
			{
				if (resObj == null)
					continue;

				string name = GetResourceField(resObj, "Name");
				if (string.IsNullOrEmpty(name))
					continue;

				float baseEfficiency = GetResourceField(resObj, "BaseEfficiency", 0f);
				double minHarvest = GetResourceField(resObj, "MinHarvestValue", 0d);
				double density = GetResourceField(resObj, "density", 0.05d);
				if (density <= double.Epsilon)
					density = 0.05d;

				double sample = SampleSpaceDustResource(name, vessel.mainBody, altitude, vessel.latitude, vessel.longitude);
				double rate = sample * intakeVolume * baseEfficiency * scale / density;
				if (rate <= minHarvest)
					continue;

				resourceChangeRequest.Add(new KeyValuePair<string, double>(name, rate));
			}
		}

		private static int GetHarvestType(PartModule harvester)
		{
			FieldInfo field = harvester.GetType().GetField("HarvestType", BindingFlags.Instance | BindingFlags.Public);
			if (field == null)
				return 0;

			object value = field.GetValue(harvester);
			return value != null ? (int)value : 0;
		}

		private static double ComputeIntakeVolume(PartModule harvester, int harvestType)
		{
			Vessel vessel = harvester.vessel;
			if (vessel == null)
				return 0d;

			float intakeArea = BridgeModuleFields.GetFloat(harvester, "IntakeArea");
			float intakeSpeedStatic = BridgeModuleFields.GetFloat(harvester, "IntakeSpeedStatic");
			FloatCurve intakeVelocityScale = BridgeModuleFields.GetField(harvester, "IntakeVelocityScale", new FloatCurve());
			Transform intakeTransform = BridgeModuleFields.GetField<Transform>(harvester, "HarvestIntakeTransform", null);
			if (intakeTransform == null)
				intakeTransform = harvester.part.transform;

			// Atmosphere = 0, Exosphere = 1, Omni = 2
			if (harvestType == 0)
			{
				if (vessel.atmDensity <= 0d)
					return 0d;

				Vector3d worldVelocity = vessel.srf_velocity;
				double mach = vessel.mach;
				double dot = Vector3d.Dot(worldVelocity, intakeTransform.forward);
				return (worldVelocity.magnitude * Math.Max(dot, 0d) * intakeVelocityScale.Evaluate((float)mach) + intakeSpeedStatic) * intakeArea;
			}

			if (harvestType == 1)
			{
				if (vessel.atmDensity > 0d)
					return 0d;

				Vector3d worldVelocity = vessel.obt_velocity;
				double dot = Vector3d.Dot(worldVelocity.normalized, intakeTransform.forward.normalized);
				return (worldVelocity.magnitude * Math.Max(dot, 0d) + intakeSpeedStatic) * intakeArea;
			}

			return intakeSpeedStatic * intakeArea;
		}

		private static double SampleSpaceDustResource(string resourceName, CelestialBody body, double altitude, double latitude, double longitude)
		{
			Type mapType = AccessTools.TypeByName("SpaceDust.SpaceDustResourceMap");
			if (mapType == null)
				return 0d;

			PropertyInfo instanceProp = mapType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
			object map = instanceProp?.GetValue(null, null);
			if (map == null)
				return 0d;

			MethodInfo sample = mapType.GetMethod("SampleResource", BindingFlags.Instance | BindingFlags.Public);
			if (sample == null)
				return 0d;

			object result = sample.Invoke(map, new object[] { resourceName, body, altitude, latitude, longitude });
			return result is double d ? d : 0d;
		}

		private static string GetResourceField(object resObj, string fieldName)
		{
			FieldInfo field = resObj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			return field?.GetValue(resObj) as string ?? string.Empty;
		}

		private static float GetResourceField(object resObj, string fieldName, float fallback)
		{
			FieldInfo field = resObj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			return field != null ? (float)field.GetValue(resObj) : fallback;
		}

		private static double GetResourceField(object resObj, string fieldName, double fallback)
		{
			FieldInfo field = resObj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			return field != null ? (double)field.GetValue(resObj) : fallback;
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
			ProtoPartModuleSnapshot harvesterSnapshot = BridgeUtils.TryFindPartModuleSnapshot(part_snapshot, "ModuleSpaceDustHarvester");
			if (harvesterSnapshot != null && Lib.Proto.GetBool(harvesterSnapshot, "Enabled"))
			{
				PartModule harvesterPrefab = FindSpaceDustHarvesterPrefab(proto_part);
				float powerCost = harvesterPrefab != null
					? BridgeModuleFields.GetFloat(harvesterPrefab, "PowerCost")
					: 0f;
				if (powerCost > 0f)
					resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -powerCost * elapsed_s));
			}

			SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
			return brokerTitle;
		}

		private static PartModule FindSpaceDustHarvesterPrefab(Part proto_part)
		{
			foreach (PartModule module in proto_part.Modules)
			{
				if (module.moduleName == "ModuleSpaceDustHarvester")
					return module;
			}

			return null;
		}
	}
}
