using System;
using System.Collections.Generic;
using System.Reflection;
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

		private static readonly MethodInfo DoFocusedHarvestingMethod =
			typeof(ModuleSpaceDustHarvester).GetMethod("DoFocusedHarvesting", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

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
			return EvaluateThermalScale(harvester.SystemEfficiency, loopTemp);
		}

		private static double EvaluateThermalScale(FloatCurve efficiencyCurve, float loopTemperatureK)
		{
			float thermal = efficiencyCurve != null ? efficiencyCurve.Evaluate(loopTemperatureK) : 1f;
			return Mathf.Clamp(thermal, 0f, 1f);
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

		/// <summary>Native FixedUpdate prepays EC for the whole physics step; use ~1s for Kerbalism UI sync.</summary>
		internal static bool HasOperatingPower(ModuleSpaceDustHarvester harvester, Vessel v)
		{
			if (harvester == null || v == null || !harvester.Enabled)
				return false;

			if (harvester.PowerCost <= 0f)
				return true;

			ResourceInfo ec = KERBALISM.ResourceCache.GetResource(v, "ElectricCharge");
			return ec.Amount >= harvester.PowerCost + harvester.minResToLeave;
		}

		internal static bool IsThermallyShutdown(ModuleSpaceDustHarvester harvester)
		{
			if (harvester == null || harvester.part == null || string.IsNullOrEmpty(harvester.HeatModuleID))
				return false;

			foreach (PartModule module in harvester.part.Modules)
			{
				if (module.moduleName != "ModuleSystemHeat")
					continue;
				if (BridgeModuleFields.GetString(module, "moduleID") != harvester.HeatModuleID)
					continue;

				float loopTemp = BridgeModuleFields.GetFloat(module, "currentLoopTemperature");
				return loopTemp > harvester.ShutdownTemperature;
			}

			return false;
		}

		internal static double GetLoadedThermalScale(ModuleSpaceDustHarvester harvester)
		{
			if (harvester == null || harvester.part == null || string.IsNullOrEmpty(harvester.HeatModuleID))
				return 1d;

			foreach (PartModule module in harvester.part.Modules)
			{
				if (module.moduleName != "ModuleSystemHeat")
					continue;
				if (BridgeModuleFields.GetString(module, "moduleID") != harvester.HeatModuleID)
					continue;

				float loopTemp = BridgeModuleFields.GetFloat(module, "currentLoopTemperature");
				return EvaluateThermalScale(harvester.SystemEfficiency, loopTemp);
			}

			return 1d;
		}

		/// <summary>Correct native harvest UI after FixedUpdate when Kerbalism owns EC/harvest rates.</summary>
		internal static void SyncNativeUiAfterFixedUpdate(ModuleSpaceDustHarvester harvester)
		{
			if (harvester == null || harvester.vessel == null || !harvester.Enabled)
				return;

			if (!HasOperatingPower(harvester, harvester.vessel) || IsThermallyShutdown(harvester))
				return;

			DoFocusedHarvestingMethod?.Invoke(harvester, new object[] { GetLoadedThermalScale(harvester) });

			harvester.ScannerUI = Localizer.Format("#LOC_SpaceDust_ModuleSpaceDustHarvester_Field_Resources_Harvesting");
			harvester.Fields["IntakeSpeed"].guiActive = true;
			harvester.Fields["ScoopUI"].guiActive = true;

			if (string.IsNullOrEmpty(harvester.HeatModuleID))
				return;

			foreach (PartModule module in harvester.part.Modules)
			{
				if (module.moduleName != "ModuleSystemHeat")
					continue;
				if (BridgeModuleFields.GetString(module, "moduleID") != harvester.HeatModuleID)
					continue;

				float loopTemp = BridgeModuleFields.GetFloat(module, "currentLoopTemperature");
				float efficiencyPct = (float)(EvaluateThermalScale(harvester.SystemEfficiency, loopTemp) * 100d);
				harvester.Fields["ThermalUI"].guiActive = true;
				harvester.ThermalUI = Localizer.Format(
					"#LOC_SpaceDust_ModuleSpaceDustHarvester_Field_Thermal_Running",
					efficiencyPct.ToString("F1"));
				return;
			}
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			SyncProtoState();
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

		private void SyncProtoState()
		{
			ModuleSpaceDustHarvester harvester = NativeHarvester;
			if (harvester == null)
				return;

			ProtoPartSnapshot partSnapshot = part.protoPartSnapshot;
			if (partSnapshot == null)
				return;

			ProtoPartModuleSnapshot harvesterSnapshot = FindHarvesterSnapshot(partSnapshot, part.partInfo.partPrefab, harvesterModuleID);
			if (harvesterSnapshot != null)
				Lib.Proto.Set(harvesterSnapshot, "Enabled", harvester.Enabled);
		}

		internal static void AddBackgroundHarvestRates(
			Vessel v,
			ModuleSpaceDustHarvester harvesterPrefab,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			ProtoPartSnapshot partSnapshot,
			Part partPrefab,
			string harvesterModuleId)
		{
			if (v == null || harvesterPrefab == null || partSnapshot == null)
				return;

			ProtoPartModuleSnapshot harvesterSnapshot = FindHarvesterSnapshot(partSnapshot, partPrefab, harvesterModuleId);
			if (harvesterSnapshot == null || !IsHarvesterEnabledInProto(harvesterSnapshot))
				return;

			// Atmospheric ram scoops need loaded flight physics; background sim cannot model them reliably.
			if (harvesterPrefab.HarvestType == HarvestType.Atmosphere)
				return;

			if (!HasBackgroundOperatingPower(v, harvesterPrefab))
				return;

			double scale = GetBackgroundThermalScale(partSnapshot, harvesterPrefab);
			float powerCost = harvesterPrefab.PowerCost;
			if (powerCost > 0f)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -powerCost * scale));

			// Exosphere harvesters (e.g. PK-EXO): background cannot resolve intake orientation; assume ideal alignment.
			AddHarvestRatesFromModule(harvesterPrefab, v, resourceChangeRequest, scale, intakeAlignment: 1d);
		}

		private static bool IsHarvesterEnabledInProto(ProtoPartModuleSnapshot snapshot)
		{
			if (snapshot == null)
				return false;

			string raw = Lib.Proto.GetString(snapshot, "Enabled");
			if (string.IsNullOrEmpty(raw))
				return Lib.Proto.GetBool(snapshot, "Enabled");

			if (bool.TryParse(raw, out bool enabled))
				return enabled;

			return raw == "1";
		}

		private static bool HasBackgroundOperatingPower(Vessel v, ModuleSpaceDustHarvester harvesterPrefab)
		{
			if (harvesterPrefab.PowerCost <= 0f)
				return true;

			ResourceInfo ec = KERBALISM.ResourceCache.GetResource(v, "ElectricCharge");
			return ec.Amount >= harvesterPrefab.PowerCost + harvesterPrefab.minResToLeave;
		}

		private static ProtoPartModuleSnapshot FindHarvesterSnapshot(ProtoPartSnapshot part, Part partPrefab, string harvesterModuleId)
		{
			if (part == null)
				return null;

			var prefabData = new Dictionary<string, Lib.Module_prefab_data>();
			ProtoPartModuleSnapshot fallback = null;
			foreach (ProtoPartModuleSnapshot module in part.modules)
			{
				PartModule prefabModule = partPrefab != null
					? Lib.ModulePrefab(partPrefab.Modules, module.moduleName, prefabData)
					: null;
				bool isHarvester = module.moduleName == nameof(ModuleSpaceDustHarvester)
					|| prefabModule is ModuleSpaceDustHarvester;
				if (!isHarvester)
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

				return EvaluateThermalScale(harvesterPrefab.SystemEfficiency, loopTemp);
			}

			return 1d;
		}

		private static void AddHarvestRatesFromModule(
			ModuleSpaceDustHarvester harvester,
			Vessel v,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			double scale,
			double intakeAlignment = double.NaN)
		{
			List<HarvestedResource> resources = harvester.resources;
			if (resources == null || resources.Count == 0)
				return;

			double intakeVolume = ComputeIntakeVolume(harvester, v, intakeAlignment);
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

		private static Transform FindIntakeTransform(ModuleSpaceDustHarvester harvester)
		{
			if (harvester?.part == null)
				return null;

			if (!string.IsNullOrEmpty(harvester.HarvestIntakeTransformName))
			{
				Transform intakeTransform = harvester.part.FindModelTransform(harvester.HarvestIntakeTransformName);
				if (intakeTransform != null)
					return intakeTransform;
			}

			return harvester.part.transform;
		}

		private static Vector3d GetExosphereOrbitalVelocity(Vessel v)
		{
			if (v == null)
				return Vector3d.zero;

			Vector3d velocity = v.obt_velocity;
			if (velocity.sqrMagnitude > 1e-6)
				return velocity;

			if (v.orbit != null && v.orbit.vel.sqrMagnitude > 1e-6)
				return v.orbit.vel;

			return Vector3d.zero;
		}

		private static double ComputeIntakeVolume(ModuleSpaceDustHarvester harvester, Vessel v, double intakeAlignment = double.NaN)
		{
			if (harvester == null || v == null || v.mainBody == null)
				return 0d;

			bool useCachedAlignment = !double.IsNaN(intakeAlignment);
			Transform intakeTransform = useCachedAlignment ? null : FindIntakeTransform(harvester);

			if (harvester.HarvestType == HarvestType.Atmosphere)
			{
				if (v.atmDensity <= 0d)
					return 0d;

				Vector3d worldVelocity = v.srf_velocity;
				double mach = v.mach;
				double alignment = useCachedAlignment
					? intakeAlignment
					: (intakeTransform != null
						? Math.Max(Vector3d.Dot(worldVelocity, intakeTransform.forward), 0d)
						: Math.Max(worldVelocity.magnitude, 0d));
				return (worldVelocity.magnitude * alignment * harvester.IntakeVelocityScale.Evaluate((float)mach) + harvester.IntakeSpeedStatic) * harvester.IntakeArea;
			}

			if (harvester.HarvestType == HarvestType.Exosphere)
			{
				if (v.atmDensity > 0d)
					return 0d;

				Vector3d worldVelocity = GetExosphereOrbitalVelocity(v);
				double alignment = useCachedAlignment
					? intakeAlignment
					: (intakeTransform != null
						? Math.Max(Vector3d.Dot(worldVelocity.normalized, intakeTransform.forward.normalized), 0d)
						: 1d);
				return (worldVelocity.magnitude * alignment + harvester.IntakeSpeedStatic) * harvester.IntakeArea;
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
			ModuleSpaceDustHarvester harvesterPrefab = FindHarvesterPrefab(proto_part, harvesterModuleId);

			AddBackgroundHarvestRates(v, harvesterPrefab, resourceChangeRequest, part_snapshot, proto_part, harvesterModuleId);
			SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
			return brokerTitle;
		}

		private static ModuleSpaceDustHarvester FindHarvesterPrefab(Part protoPart, string harvesterModuleId)
		{
			if (protoPart == null)
				return null;

			ModuleSpaceDustHarvester fallback = null;
			for (int i = 0; i < protoPart.Modules.Count; i++)
			{
				ModuleSpaceDustHarvester harvester = protoPart.Modules[i] as ModuleSpaceDustHarvester;
				if (harvester == null)
					continue;

				if (fallback == null)
					fallback = harvester;

				if (string.IsNullOrEmpty(harvesterModuleId) || harvester.ModuleID == harvesterModuleId)
					return harvester;
			}

			return fallback;
		}
	}
}
