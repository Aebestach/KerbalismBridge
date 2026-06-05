using KSP.Localization;
using System.Collections.Generic;
using FarFutureTechnologies;
using KERBALISM;
using SystemHeat;

namespace KerbalismFFT
{
	class FFTFusionEngineKerbalismUpdater : PartModule, IKerbalismModule
	{
		public static string brokerName = "FFTFusionEngine";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismFFT_Brokers_FusionEngine");

		[KSPField(isPersistant = false)]
		public bool FirstLoad = true;

		[KSPField(isPersistant = true)]
		public string engineModuleID = "";

		[KSPField(isPersistant = true)]
		public int lastReactorModeIndex = 0;
		[KSPField(isPersistant = true)]
		public float MaxECGeneration = 0f;
		[KSPField(isPersistant = true)]
		public float MinThrottle = 0.1f;

		protected static string engineModuleName = "ModuleFusionEngine";
		protected ModuleFusionEngine engineModule;

		internal FusionReactor EngineModule => engineModule;

		protected List<FusionReactorMode> modes;
		protected bool modesListParsed = false;
		private bool lastPlannerCharging;

		internal void EnsureModesParsed()
		{
			if (!modesListParsed)
				ParseModesList(part);
		}

		public virtual void Start()
		{
			if (Lib.IsFlight() || Lib.IsEditor())
			{
				if (engineModule == null)
					engineModule = FindEngineModule(part, engineModuleID);

				if (FirstLoad)
				{
					if (engineModule != null)
					{
						MinThrottle = engineModule.MinimumReactorPower;
						ParseModesList(part);
						MaxECGeneration = modes[lastReactorModeIndex].powerGeneration;
					}
					FirstLoad = false;
				}
			}
		}

		protected void ParseModesList(Part part)
		{
			if (modesListParsed)
				return;

			ConfigNode node = ModuleUtils.GetModuleConfigNode(part, engineModuleName);
			if (node != null)
			{
				ConfigNode[] varNodes = node.GetNodes("FUSIONMODE");
				modes = new List<FusionReactorMode>();
				for (int i = 0; i < varNodes.Length; i++)
					modes.Add(new FusionReactorMode(varNodes[i]));
			}
			modesListParsed = true;
		}

		public virtual void FixedUpdate()
		{
			if (engineModule != null)
			{
				if (lastReactorModeIndex != engineModule.currentModeIndex)
				{
					lastReactorModeIndex = engineModule.currentModeIndex;
					if (Lib.IsEditor())
						KFFTUtils.UpdateKerbalismPlannerUINow();
					EnsureModesParsed();
					MaxECGeneration = modes[lastReactorModeIndex].powerGeneration;
				}

				if (Lib.IsFlight() && engineModule.Enabled)
				{
					FusionReactorResourceSim.UpdateLoadedThrottle(engineModule);
					FusionReactorResourceSim.ValidateLoadedReactor(engineModule, vessel);
				}

				bool plannerCharging = !engineModule.Enabled && engineModule.Charging && !engineModule.Charged;
				if (plannerCharging != lastPlannerCharging)
				{
					lastPlannerCharging = plannerCharging;
					KFFTUtils.UpdateKerbalismPlannerUINow();
				}
			}
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (engineModule == null)
				engineModule = FindEngineModule(part, engineModuleID);
			if (FusionReactorResourceSim.UpdateLoadedCharge(engineModule, vessel, brokerName, brokerTitle))
				return brokerTitle;
			return FusionReactorResourceSim.AddLoadedRates(engineModule, resourceChangeRequest, brokerTitle);
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (engineModule == null)
				engineModule = FindEngineModule(part, engineModuleID);
			if (engineModule != null)
			{
				EnsureModesParsed();
				return FusionReactorResourceSim.AddPlannerRates(
					engineModule,
					resourceChangeRequest,
					brokerTitle,
					MaxECGeneration,
					lastReactorModeIndex,
					modes);
			}
			return "ERR: no engine";
		}

		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			ProtoPartModuleSnapshot reactor = KFFTUtils.FindPartModuleSnapshot(part_snapshot, engineModuleName);
			if (reactor != null)
			{
				FusionReactorResourceSim.BackgroundCharge(v, reactor, proto_part, resourceChangeRequest, elapsed_s);
				SystemHeatBackgroundBridge.TryRun(v, elapsed_s);

				if (Lib.Proto.GetBool(reactor, "Enabled"))
				{
					float maxECGeneration = Lib.Proto.GetFloat(module_snapshot, "MaxECGeneration");
					float minThrottle = Lib.Proto.GetFloat(module_snapshot, "MinThrottle");
					int modeIndex = Lib.Proto.GetInt(module_snapshot, "lastReactorModeIndex");
					bool needToStopReactor = false;
					float curThrottle = 1.0f;

					if (maxECGeneration > 0)
					{
						VesselResources resources = KERBALISM.ResourceCache.Get(v);
						var updater = proto_part_module as FFTFusionEngineKerbalismUpdater;
						if (!updater.modesListParsed)
							updater.ParseModesList(proto_part);

						if (minThrottle > 0)
						{
							ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
								brokerName,
								KERBALISM.ResourceBroker.BrokerCategory.Converter,
								brokerTitle));
							foreach (ResourceRatio ir in updater.modes[modeIndex].inputs)
							{
								recipe.AddInput(ir.ResourceName, ir.Ratio * minThrottle * elapsed_s);
								if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
									needToStopReactor = true;
							}
							recipe.AddOutput("ElectricCharge", minThrottle * maxECGeneration * elapsed_s, dump: true);
							resources.AddRecipe(recipe);
						}

						if (!needToStopReactor)
						{
							curThrottle -= minThrottle;
							if (curThrottle > 0)
							{
								ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
									brokerName,
									KERBALISM.ResourceBroker.BrokerCategory.Converter,
									brokerTitle));
								foreach (ResourceRatio ir in updater.modes[modeIndex].inputs)
								{
									recipe.AddInput(ir.ResourceName, ir.Ratio * curThrottle * elapsed_s);
									if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
										needToStopReactor = true;
								}
								recipe.AddOutput("ElectricCharge", curThrottle * maxECGeneration * elapsed_s, dump: false);
								resources.AddRecipe(recipe);
							}
						}
					}

					if (needToStopReactor)
					{
						Lib.Proto.Set(reactor, "Enabled", false);
						Lib.Proto.Set(reactor, "CurrentCharge", 0f);
						Lib.Proto.Set(reactor, "Charged", false);
					}
				}
				return brokerTitle;
			}
			return "ERR: no engine";
		}

		public ModuleFusionEngine FindEngineModule(Part part, string moduleName)
		{
			ModuleFusionEngine firstEngine = null;
			for (int i = 0; i < part.Modules.Count; i++)
			{
				ModuleFusionEngine engine = part.Modules[i] as ModuleFusionEngine;
				if (engine == null)
					continue;

				if (firstEngine == null)
					firstEngine = engine;

				if (engine.ModuleID == moduleName)
				{
					engineModule = engine;
					return engine;
				}
			}

			if (firstEngine != null)
				KFFTUtils.LogError($"[{part}] No ModuleFusionEngine named {moduleName} was found, using first instance.");
			else
				KFFTUtils.LogError($"[{part}] No ModuleFusionEngine was found.");

			engineModule = firstEngine;
			return firstEngine;
		}
	}
}
