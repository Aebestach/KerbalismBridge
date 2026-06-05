using System.Collections.Generic;
using System.Reflection;
using KSP.Localization;
using KERBALISM;
using SystemHeat;

namespace KerbalismNative
{
	/// <summary>
	/// Shared fission reactor resource logic for Kerbalism background, loaded ResourceUpdate, and validation.
	/// </summary>
	internal static class FissionReactorResourceSim
	{
		private static readonly MethodInfo CalculateGoalThrottleMethod =
			typeof(ModuleSystemHeatFissionReactor).GetMethod("CalculateGoalThrottle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		private static readonly FieldInfo FuelCheckPassedField =
			typeof(ModuleSystemHeatFissionReactor).GetField("fuelCheckPassed", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo BurnRateField =
			typeof(ModuleSystemHeatFissionReactor).GetField("burnRate", BindingFlags.Instance | BindingFlags.NonPublic);

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
			{
				SyncLoadedReactorStatus(reactor, false, 0f, 0f, updater?.Inputs);
				return SystemHeatFissionReactorKerbalismUpdater.brokerTitle;
			}

			updater.EnsureResourcesParsed();

			float fuelThrottle = reactor.CurrentReactorThrottle / 100f;
			if (fuelThrottle <= 0f)
			{
				SyncLoadedReactorStatus(reactor, false, 0f, 0f, updater.Inputs);
				return SystemHeatFissionReactorKerbalismUpdater.brokerTitle;
			}

			float ecRate = (float)reactor.ElectricalGeneration.Evaluate(reactor.CurrentThrottle);
			if (ecRate > 0f)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", ecRate));
			SyncLoadedReactorStatus(reactor, true, fuelThrottle, ecRate, updater.Inputs);

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
			ModuleSystemHeatFissionReactor reactor = updater.EngineModule;
			if (reactor == null || !reactor.Enabled)
			{
				SyncLoadedReactorStatus(reactor, false, 0f, 0f, updater?.Inputs);
				return SystemHeatFissionEngineKerbalismUpdater.brokerTitle;
			}

			updater.EnsureResourcesParsed();

			float fuelThrottle = reactor.CurrentReactorThrottle / 100f;
			if (fuelThrottle <= 0f)
			{
				SyncLoadedReactorStatus(reactor, false, 0f, 0f, updater.Inputs);
				return SystemHeatFissionEngineKerbalismUpdater.brokerTitle;
			}

			float ecRate = 0f;
			if (updater.GeneratesElectricity && reactor.GeneratesElectricity)
			{
				ecRate = (float)reactor.ElectricalGeneration.Evaluate(reactor.CurrentThrottle);
				if (ecRate > 0f)
					resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", ecRate));
			}
			SyncLoadedReactorStatus(reactor, true, fuelThrottle, ecRate, updater.Inputs);

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
							"#LOC_KerbalismBridge_ReactorOutputResourceFull",
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

		private static void SyncLoadedReactorStatus(ModuleSystemHeatFissionReactor reactor, bool fuelCheckPassed, float fuelThrottle, float currentElectricalGeneration, List<ResourceRatio> inputs)
		{
			if (reactor == null)
				return;

			reactor.CurrentElectricalGeneration = currentElectricalGeneration;
			reactor.MaxElectricalGeneration = reactor.ManualControl
				? currentElectricalGeneration
				: (float)reactor.ElectricalGeneration.Evaluate(100f) * reactor.CoreIntegrity / 100f;

			FuelCheckPassedField?.SetValue(reactor, fuelCheckPassed);
			BurnRateField?.SetValue(reactor, fuelCheckPassed ? GetFuelBurnRate(reactor, fuelThrottle, inputs) : 0d);
		}

		private static double GetFuelBurnRate(ModuleSystemHeatFissionReactor reactor, float fuelThrottle, List<ResourceRatio> inputs)
		{
			if (inputs == null)
				return 0d;

			foreach (ResourceRatio input in inputs)
			{
				if (input.ResourceName == reactor.FuelName)
					return fuelThrottle * input.Ratio;
			}

			return 0d;
		}
	}
}
