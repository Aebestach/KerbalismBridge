using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;
using KERBALISM;
using SystemHeat;
using KerbalismBridge;

namespace KerbalismNative
{
	public class SystemHeatRadiatorKerbalism: ModuleSystemHeatRadiator
	{
		[KSPField(isPersistant = true)]
		public float scale = 1f;

		[KSPField(isPersistant = true)]
		public float scaleEmissionPower = 2f;

		public static string radiatorTitle = Localizer.Format("#LOC_KerbalismBridge_Radiator");

		public List<ModuleResource> inputResourcesClone;

		FloatCurve baseTemperatureCurve;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			inputResourcesClone = resHandler.inputResources.ConvertAll(p => p);
			EnsureBaseTemperatureCurve();
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
			EnsureBaseTemperatureCurve();
			if (scale != 1f)
				RebuildTemperatureCurve();
		}

		void EnsureBaseTemperatureCurve()
		{
			if (baseTemperatureCurve != null && baseTemperatureCurve.Curve.length > 0)
				return;

			ModuleSystemHeatRadiator prefabRadiator = part.partInfo.partPrefab.FindModuleImplementing<ModuleSystemHeatRadiator>();
			FloatCurve source = prefabRadiator != null ? prefabRadiator.temperatureCurve : temperatureCurve;
			baseTemperatureCurve = CloneCurve(source);
		}

		static FloatCurve CloneCurve(FloatCurve source)
		{
			FloatCurve clone = new FloatCurve();
			if (source == null)
				return clone;

			for (int i = 0; i < source.Curve.length; i++)
			{
				Keyframe key = source.Curve.keys[i];
				clone.Add(key.time, key.value);
			}
			return clone;
		}

		void RebuildTemperatureCurve()
		{
			EnsureBaseTemperatureCurve();
			if (baseTemperatureCurve.Curve.length == 0)
				return;

			temperatureCurve = new FloatCurve();
			float scaleFactor = (float)Math.Pow(scale, scaleEmissionPower);
			for (int i = 0; i < baseTemperatureCurve.Curve.length; i++)
			{
				Keyframe key = baseTemperatureCurve.Curve.keys[i];
				temperatureCurve.Add(key.time, key.value * scaleFactor);
			}
		}

		// Tweakscale support
		[KSPEvent]
		void OnPartScaleChanged(BaseEventDetails data)
		{
			scale = data.Get<float>("factorAbsolute");
			RebuildTemperatureCurve();
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			// note: IsCooling is not valid in the editor, for deployable radiators,
			// we will have to check if the related deploy module is deployed
			// we use PlannerController instead
			foreach (ModuleResource res in resHandler.inputResources)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate * Math.Pow(scale, scaleEmissionPower)));
			}
			return radiatorTitle;
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			if (Lib.Proto.GetBool(module_snapshot, "IsCooling"))
			{
				float scale = Lib.Proto.GetFloat(module_snapshot, "scale");
				float scaleEmissionPower = Lib.Proto.GetFloat(module_snapshot, "scaleEmissionPower");
				foreach (ModuleResource res in ((proto_part_module as SystemHeatRadiatorKerbalism).resHandler.inputResources))
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate * Math.Pow(scale, scaleEmissionPower)));
				}
			}

			SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
			return radiatorTitle;
		}

		// Simulate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (IsCooling)
			{
				foreach (ModuleResource res in resHandler.inputResources)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(res.name, -res.rate * Math.Pow(scale, scaleEmissionPower)));
				}
			}
			return radiatorTitle;
		}

		public override void FixedUpdate()
		{
			// Temporary set input resources list to empty to prevent resources consumption in FixedUpdate
			// Input resources consumption is handled by ResourceUpdate
			resHandler.inputResources = new List<ModuleResource>();
			base.FixedUpdate();
			resHandler.inputResources = inputResourcesClone;
		}
	}
}
