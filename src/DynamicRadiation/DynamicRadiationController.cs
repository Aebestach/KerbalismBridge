using System.Collections.Generic;
using KERBALISM;

namespace KerbalismDynamicRadiation
{
	/// <summary>
	/// Scales Kerbalism <see cref="Emitter"/> output with fission/fusion power source state.
	/// Added by ModuleManager to parts patched by zKerbalismSystemHeat and zKerbalismFFT.
	/// </summary>
	public class DynamicRadiationController : PartModule
	{
		[KSPField(isPersistant = true)]
		public string powerModuleName = "";

		[KSPField(isPersistant = true)]
		public string powerModuleId = "";

		// "enabled" = FusionReactor / ModuleFusionEngine Enabled; "thrust" = ModuleEngines throttle.
		[KSPField(isPersistant = true)]
		public string powerActiveMode = "enabled";

		[KSPField(isPersistant = true)]
		public double minEmissionPercent = 25.0;

		[KSPField(isPersistant = true)]
		public double emissionDecayRate = 3600.0;

		[KSPField(isPersistant = true)]
		public bool reactorHasStarted = false;

		[KSPField(isPersistant = true)]
		public double reactorStoppedAt = 0.0;

		[KSPField(isPersistant = true)]
		public double emitterMaxRadiation = 0.0;

		[KSPField(isPersistant = true)]
		public int emitterIndex = -1;

		[KSPField(isPersistant = true)]
		public bool initialized = false;

		Emitter emitter;

		public override void OnStart(StartState state)
		{
			if (Lib.DisableScenario(this))
				return;

			TryInitialize();
		}

		void TryInitialize()
		{
			if (initialized)
				return;

			emitter = DynamicRadiationLogic.FindPrimaryEmitter(part, ref emitterIndex);
			if (emitter != null && emitterMaxRadiation <= 0.0)
				emitterMaxRadiation = emitter.radiation;

			if (emitter != null && !reactorHasStarted)
			{
				emitter.running = false;
				double minRad = emitterMaxRadiation * minEmissionPercent / 100.0;
				emitter.radiation = minRad;
			}

			initialized = emitter != null && emitterMaxRadiation > 0.0;
		}

		public void FixedUpdate()
		{
			if (!Lib.IsFlight() || !Features.Radiation)
				return;

			if (!initialized)
				TryInitialize();

			if (!initialized || emitter == null)
				return;

			bool enabled = DynamicRadiationLogic.GetPowerEnabled(part, powerModuleName, powerModuleId, powerActiveMode);
			DynamicRadiationLogic.UpdateFlight(
				emitter,
				enabled,
				ref reactorHasStarted,
				ref reactorStoppedAt,
				emitterMaxRadiation,
				minEmissionPercent,
				emissionDecayRate);
		}

		public static string BackgroundUpdate(
			Vessel vessel,
			ProtoPartSnapshot part_snapshot,
			ProtoPartModuleSnapshot module_snapshot,
			PartModule module_prefab,
			Part part_prefab,
			Dictionary<string, double> availableResources,
			List<KeyValuePair<string, double>> resourceChangeRequest,
			double elapsed_s)
		{
			if (!Features.Radiation)
				return string.Empty;

			string powerModuleName = Lib.Proto.GetString(module_snapshot, "powerModuleName");
			string powerModuleId = Lib.Proto.GetString(module_snapshot, "powerModuleId");
			string powerActiveMode = Lib.Proto.GetString(module_snapshot, "powerActiveMode");
			if (string.IsNullOrEmpty(powerActiveMode))
				powerActiveMode = "enabled";
			double minEmissionPercent = Lib.Proto.GetDouble(module_snapshot, "minEmissionPercent");
			double emissionDecayRate = Lib.Proto.GetDouble(module_snapshot, "emissionDecayRate");
			int emitterIndex = (int)Lib.Proto.GetDouble(module_snapshot, "emitterIndex");

			double peakRadiation = Lib.Proto.GetDouble(module_snapshot, "emitterMaxRadiation");
			if (peakRadiation <= 0.0)
			{
				for (int i = 0; i < part_snapshot.modules.Count; i++)
				{
					ProtoPartModuleSnapshot pm = part_snapshot.modules[i];
					if (pm.moduleName != "Emitter")
						continue;

					double rad = Lib.Proto.GetDouble(pm, "radiation");
					if (rad > peakRadiation)
						peakRadiation = rad;
				}

				if (peakRadiation <= 0.0)
					return string.Empty;

				Lib.Proto.Set(module_snapshot, "emitterMaxRadiation", peakRadiation);
			}

			ProtoPartModuleSnapshot emitterSnapshot = DynamicRadiationLogic.FindEmitterSnapshot(
				part_snapshot,
				emitterIndex,
				peakRadiation);

			if (emitterSnapshot == null)
				return string.Empty;

			bool enabled = DynamicRadiationLogic.GetPowerEnabledProto(
				part_snapshot,
				powerModuleName,
				powerModuleId,
				powerActiveMode);

			bool started = Lib.Proto.GetBool(module_snapshot, "reactorHasStarted");
			double stoppedAt = Lib.Proto.GetDouble(module_snapshot, "reactorStoppedAt");

			DynamicRadiationLogic.UpdateBackground(
				emitterSnapshot,
				enabled,
				ref started,
				ref stoppedAt,
				peakRadiation,
				minEmissionPercent,
				emissionDecayRate,
				elapsed_s);

			Lib.Proto.Set(module_snapshot, "reactorHasStarted", started);
			Lib.Proto.Set(module_snapshot, "reactorStoppedAt", stoppedAt);

			return string.Empty;
		}
	}
}
