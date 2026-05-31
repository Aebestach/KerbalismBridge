using System.Collections.Generic;
using System.Reflection;
using FarFutureTechnologies;
using KERBALISM;

namespace KerbalismFFT
{
	internal static class FusionReactorResourceSim
	{
		private static readonly FieldInfo ReactorThrottleField =
			typeof(FusionReactor).GetField("reactorThrottle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		private static readonly FieldInfo ModesField =
			typeof(FusionReactor).GetField("modes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		private static List<FusionReactorMode> GetModes(FusionReactor reactor)
		{
			return ModesField?.GetValue(reactor) as List<FusionReactorMode>;
		}

		private static float GetThrottle(FusionReactor reactor)
		{
			return ReactorThrottleField != null ? (float)ReactorThrottleField.GetValue(reactor) : 1f;
		}

		internal static void UpdateLoadedThrottle(FusionReactor reactor)
		{
			List<FusionReactorMode> modes = GetModes(reactor);
			if (reactor == null || !reactor.Enabled || modes == null || modes.Count == 0)
				return;

			reactor.part.GetConnectedResourceTotals(PartResourceLibrary.ElectricityHashcode, out double shipEC, out double shipMaxEC, true);
			float requestedFramePower = (float)(shipMaxEC - shipEC);
			float clampedFramePower = UnityEngine.Mathf.Clamp(
				requestedFramePower,
				modes[reactor.currentModeIndex].powerGeneration * TimeWarp.fixedDeltaTime * reactor.MinimumReactorPower,
				modes[reactor.currentModeIndex].powerGeneration * TimeWarp.fixedDeltaTime);

			float requestedReactorThrottle = clampedFramePower / (modes[reactor.currentModeIndex].powerGeneration * TimeWarp.fixedDeltaTime);
			float currentThrottle = GetThrottle(reactor);
			currentThrottle = UnityEngine.Mathf.MoveTowards(currentThrottle, requestedReactorThrottle, 0.1f);
			ReactorThrottleField?.SetValue(reactor, currentThrottle);
		}

		internal static bool UpdateLoadedCharge(FusionReactor reactor, Vessel v, string brokerName, string brokerTitle)
		{
			if (reactor == null || reactor.Enabled || !reactor.Charging || reactor.Charged || reactor.ChargeRate <= 0f)
				return false;

			ResourceInfo ec = KERBALISM.ResourceCache.GetResource(v, "ElectricCharge");
			double chargeRequest = reactor.ChargeRate * TimeWarp.fixedDeltaTime;
			if (ec.Amount < chargeRequest)
				return true;

			ec.Consume(chargeRequest, KERBALISM.ResourceBroker.GetOrCreate(brokerName, KERBALISM.ResourceBroker.BrokerCategory.Converter, brokerTitle));

			float gained = UnityEngine.Mathf.Min((float)chargeRequest, reactor.ChargeGoal - reactor.CurrentCharge);
			reactor.CurrentCharge += gained;
			if (reactor.CurrentCharge >= reactor.ChargeGoal)
			{
				reactor.CurrentCharge = reactor.ChargeGoal;
				reactor.Charged = true;
			}
			return true;
		}

		internal static string AddPlannerRates(
			FusionReactor reactor,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			string brokerTitle,
			float maxEcGeneration,
			int modeIndex,
			List<FusionReactorMode> modes)
		{
			if (reactor == null)
				return brokerTitle;

			if (!reactor.Enabled && reactor.Charging && !reactor.Charged && reactor.ChargeRate > 0f)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -reactor.ChargeRate));
				return brokerTitle;
			}

			if (maxEcGeneration > 0f)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", maxEcGeneration));

			if (modes != null && modeIndex >= 0 && modeIndex < modes.Count)
			{
				foreach (ResourceRatio ratio in modes[modeIndex].inputs)
					resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, -ratio.Ratio));
			}

			return brokerTitle;
		}

		internal static string AddLoadedRates(FusionReactor reactor, List<KeyValuePair<string, double>> resourceChangeRequest, string brokerTitle)
		{
			if (reactor == null)
				return brokerTitle;

			List<FusionReactorMode> modes = GetModes(reactor);
			if (!reactor.Enabled || modes == null || modes.Count == 0)
				return brokerTitle;

			float throttle = GetThrottle(reactor);
			float power = modes[reactor.currentModeIndex].powerGeneration * throttle;
			if (power > 0f)
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", power));

			foreach (ResourceRatio input in modes[reactor.currentModeIndex].inputs)
				resourceChangeRequest.Add(new KeyValuePair<string, double>(input.ResourceName, -input.Ratio * throttle));

			return brokerTitle;
		}

		internal static void ValidateLoadedReactor(FusionReactor reactor, Vessel v)
		{
			List<FusionReactorMode> modes = GetModes(reactor);
			if (reactor == null || !reactor.Enabled || modes == null || v == null)
				return;

			VesselResources resources = KERBALISM.ResourceCache.Get(v);
			foreach (ResourceRatio input in modes[reactor.currentModeIndex].inputs)
			{
				if (resources.GetResource(v, input.ResourceName).Amount < double.Epsilon)
				{
					reactor.ReactorDeactivated();
					return;
				}
			}
		}

		internal static void BackgroundCharge(
			Vessel v,
			ProtoPartModuleSnapshot reactor,
			Part prefab,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			double elapsed_s)
		{
			if (Lib.Proto.GetBool(reactor, "Enabled"))
				return;
			if (!Lib.Proto.GetBool(reactor, "Charging") || Lib.Proto.GetBool(reactor, "Charged"))
				return;

			float chargeRate = Lib.Proto.GetFloat(reactor, "ChargeRate");
			if (chargeRate <= 0f)
				return;

			resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -chargeRate));

			double ec = KERBALISM.ResourceCache.Get(v).GetResource(v, "ElectricCharge").Amount;
			if (ec < chargeRate * elapsed_s)
				return;

			float chargeGoal = GetChargeGoal(prefab);
			float currentCharge = Lib.Proto.GetFloat(reactor, "CurrentCharge");
			currentCharge += chargeRate * (float)elapsed_s;
			if (currentCharge >= chargeGoal)
			{
				Lib.Proto.Set(reactor, "CurrentCharge", chargeGoal);
				Lib.Proto.Set(reactor, "Charged", true);
			}
			else
			{
				Lib.Proto.Set(reactor, "CurrentCharge", currentCharge);
			}
		}

		private static float GetChargeGoal(Part prefab)
		{
			FusionReactor module = prefab.FindModuleImplementing<FusionReactor>();
			return module != null ? module.ChargeGoal : 500000f;
		}
	}
}
