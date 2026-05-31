using System.Collections.Generic;
using System.Reflection;
using KSP.Localization;
using KERBALISM;
using SystemHeat;

namespace KerbalismSystemHeat
{
	/// <summary>
	/// Shared fission reactor resource logic for Kerbalism background, loaded ResourceUpdate, and validation.
	/// </summary>
	internal static class FissionReactorResourceSim
	{
		private static readonly MethodInfo CalculateGoalThrottleMethod =
			typeof(ModuleSystemHeatFissionReactor).GetMethod("CalculateGoalThrottle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		internal static void UpdateAutoThrottle(ModuleSystemHeatFissionReactor reactor, float timeStep)
		{
			if (reactor == null || !reactor.Enabled || reactor.ManualControl || CalculateGoalThrottleMethod == null)
				return;

			reactor.CurrentReactorThrottle = (float)CalculateGoalThrottleMethod.Invoke(reactor, new object[] { timeStep });
		}

		internal static string AddLoadedRates(
			SystemHeatFissionReactorKerbalismUpdater updater,
			Dictionary<string, double> availableResources,
			List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			ModuleSystemHeatFissionReactor reactor = updater.ReactorModule;
			if (reactor == null || !reactor.Enabled || !reactor.GeneratesElectricity)
				return SystemHeatFissionReactorKerbalismUpdater.brokerTitle;

			updater.EnsureResourcesParsed();

			float fuelThrottle = reactor.CurrentReactorThrottle / 100f;
			if (fuelThrottle <= 0f)
				return SystemHeatFissionReactorKerbalismUpdater.brokerTitle;

			float ecRate = (float)reactor.ElectricalGeneration.Evaluate(reactor.CurrentThrottle);
			if (ecRate > 0f)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", ecRate));

			foreach (ResourceRatio input in updater.Inputs)
				resourceChangeRequest.Add(new KeyValuePair<string, double>(input.ResourceName, -fuelThrottle * input.Ratio));

			foreach (ResourceRatio output in updater.Outputs)
				resourceChangeRequest.Add(new KeyValuePair<string, double>(output.ResourceName, fuelThrottle * output.Ratio));

			return SystemHeatFissionReactorKerbalismUpdater.brokerTitle;
		}

		internal static string AddLoadedRates(
			SystemHeatFissionEngineKerbalismUpdater updater,
			Dictionary<string, double> availableResources,
			List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (!updater.GeneratesElectricity)
				return SystemHeatFissionEngineKerbalismUpdater.brokerTitle;

			ModuleSystemHeatFissionReactor reactor = updater.EngineModule;
			if (reactor == null || !reactor.Enabled || !reactor.GeneratesElectricity)
				return SystemHeatFissionEngineKerbalismUpdater.brokerTitle;

			updater.EnsureResourcesParsed();

			float fuelThrottle = reactor.CurrentReactorThrottle / 100f;
			if (fuelThrottle <= 0f)
				return SystemHeatFissionEngineKerbalismUpdater.brokerTitle;

			float ecRate = (float)reactor.ElectricalGeneration.Evaluate(reactor.CurrentThrottle);
			if (ecRate > 0f)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", ecRate));

			foreach (ResourceRatio input in updater.Inputs)
				resourceChangeRequest.Add(new KeyValuePair<string, double>(input.ResourceName, -fuelThrottle * input.Ratio));

			foreach (ResourceRatio output in updater.Outputs)
				resourceChangeRequest.Add(new KeyValuePair<string, double>(output.ResourceName, fuelThrottle * output.Ratio));

			return SystemHeatFissionEngineKerbalismUpdater.brokerTitle;
		}

		internal static void ValidateLoadedReactor(ModuleSystemHeatFissionReactor reactor, Vessel v, List<ResourceRatio> inputs, List<ResourceRatio> outputs, string brokerTitle, string partTitle)
		{
			if (reactor == null || !reactor.Enabled || v == null)
				return;

			float fuelThrottle = reactor.CurrentReactorThrottle / 100f;
			if (fuelThrottle <= 0f)
				return;

			VesselResources resources = KERBALISM.ResourceCache.Get(v);
			bool needToStop = false;

			foreach (ResourceRatio input in inputs)
			{
				if (resources.GetResource(v, input.ResourceName).Amount < double.Epsilon)
					needToStop = true;
			}

			foreach (ResourceRatio output in outputs)
			{
				if (1 - resources.GetResource(v, output.ResourceName).Level < double.Epsilon)
				{
					needToStop = true;
					Message.Post(
						Severity.warning,
						Localizer.Format(
							"#LOC_KerbalismSystemHeat_ReactorOutputResourceFull",
							output.ResourceName,
							v.GetDisplayName(),
							partTitle)
					);
				}
			}

			if (needToStop)
				reactor.ReactorDeactivated();
		}

		internal static float GetWasteHeatKw(ModuleSystemHeatFissionReactor reactor)
		{
			if (reactor == null || !reactor.Enabled)
				return 0f;

			float heatGen = (float)reactor.HeatGeneration.Evaluate(reactor.CurrentThrottle);
			float elecGen = (float)reactor.ElectricalGeneration.Evaluate(reactor.CurrentThrottle);
			return System.Math.Max(0f, heatGen - elecGen);
		}
	}
}
