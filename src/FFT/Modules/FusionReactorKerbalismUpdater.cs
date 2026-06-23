using KSP.Localization;
using System.Collections.Generic;
using FarFutureTechnologies;
using KERBALISM;
using SystemHeat;

namespace KerbalismFFT
{
	class FFTFusionReactorKerbalismUpdater : PartModule, IKerbalismModule
	{
		public static string brokerName = "FFTFusionReactor";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismFFT_Brokers_FusionReactor");

		[KSPField(isPersistant = false)]
		public bool FirstLoad = true;

		// This should correspond to the related FusionReactor module
		[KSPField(isPersistant = true)]
		public string reactorModuleID = "";

		[KSPField(isPersistant = true)]
		public int lastReactorModeIndex = 0;
		[KSPField(isPersistant = true)]
		public float MaxECGeneration = 0f;
		[KSPField(isPersistant = true)]
		public float MinThrottle = 0.1f;

		protected static string reactorModuleName = "FusionReactor";
		protected FusionReactor reactorModule;

		internal FusionReactor ReactorModule => reactorModule;

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
				if (reactorModule == null)
				{
					reactorModule = FindReactorModule(part, reactorModuleID);
				}
				if (FirstLoad)
				{
					if (reactorModule != null)
					{
						MinThrottle = reactorModule.MinimumReactorPower;
						ParseModesList(part);
						MaxECGeneration = modes[lastReactorModeIndex].powerGeneration;
						FusionReactorResourceSim.SyncLoadedChargeUI(reactorModule, false);
					}
					FirstLoad = false;
				}
			}
		}

		// Fetch modes list from fusion reactor ConfigNode
		protected void ParseModesList(Part part)
		{
			if (!modesListParsed)
			{
				ConfigNode node = ModuleUtils.GetModuleConfigNode(part, reactorModuleName);
				if (node != null)
				{
					ConfigNode[] varNodes = node.GetNodes("FUSIONMODE");
					modes = new List<FusionReactorMode>();
					for (int i = 0; i < varNodes.Length; i++)
					{
						modes.Add(new FusionReactorMode(varNodes[i]));
					}
				}
				modesListParsed = true;
			}
		}

		public virtual void FixedUpdate()
		{
			if (reactorModule != null)
			{
				if (lastReactorModeIndex != reactorModule.currentModeIndex)
				{
					lastReactorModeIndex = reactorModule.currentModeIndex;
					if (Lib.IsEditor())
						KFFTUtils.UpdateKerbalismPlannerUINow();
					EnsureModesParsed();
					MaxECGeneration = modes[lastReactorModeIndex].powerGeneration;
				}

				if (Lib.IsFlight() && reactorModule.Enabled)
				{
					FusionReactorResourceSim.UpdateLoadedThrottle(reactorModule);
					FusionReactorResourceSim.ValidateLoadedReactor(reactorModule, vessel);
				}
				else if (Lib.IsFlight())
				{
					bool hasPower = false;
					if (reactorModule.Charging && !reactorModule.Charged)
					{
						ResourceInfo ec = KERBALISM.ResourceCache.GetResource(vessel, "ElectricCharge");
						hasPower = FusionReactorResourceSim.HasChargeOperatingPower(ec, reactorModule.ChargeRate);
					}
					FusionReactorResourceSim.SyncLoadedChargeUI(reactorModule, hasPower);
				}

				bool plannerCharging = !reactorModule.Enabled && reactorModule.Charging && !reactorModule.Charged;
				if (plannerCharging != lastPlannerCharging)
				{
					lastPlannerCharging = plannerCharging;
					KFFTUtils.UpdateKerbalismPlannerUINow();
				}
			}
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (reactorModule == null)
				reactorModule = FindReactorModule(part, reactorModuleID);
			if (FusionReactorResourceSim.UpdateLoadedCharge(reactorModule, vessel, brokerName, brokerTitle))
				return brokerTitle;
			return FusionReactorResourceSim.AddLoadedRates(reactorModule, resourceChangeRequest, brokerTitle);
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (reactorModule == null)
				reactorModule = FindReactorModule(part, reactorModuleID);
			if (reactorModule != null)
			{
				EnsureModesParsed();
				return FusionReactorResourceSim.AddPlannerRates(
					reactorModule,
					resourceChangeRequest,
					brokerTitle,
					MaxECGeneration,
					lastReactorModeIndex,
					modes);
			}
			return "ERR: no reactor";
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			ProtoPartModuleSnapshot reactor = KFFTUtils.FindPartModuleSnapshot(part_snapshot, reactorModuleName);
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
						if (!(proto_part_module as FFTFusionReactorKerbalismUpdater).modesListParsed)
						{
							(proto_part_module as FFTFusionReactorKerbalismUpdater).ParseModesList(proto_part);
						}

						// Mininum reactor throttle
						// Some input/output resources will always be consumed/produced as long as minThrottle > 0
						if (minThrottle > 0)
						{
							ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
								brokerName,
								KERBALISM.ResourceBroker.BrokerCategory.Converter,
								brokerTitle));
							foreach (ResourceRatio ir in (proto_part_module as FFTFusionReactorKerbalismUpdater).modes[modeIndex].inputs)
							{
								recipe.AddInput(ir.ResourceName, ir.Ratio * minThrottle * elapsed_s);
								if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
								{
									// Input resource amount is zero - stop reactor
									needToStopReactor = true;
								}
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
								foreach (ResourceRatio ir in (proto_part_module as FFTFusionReactorKerbalismUpdater).modes[modeIndex].inputs)
								{
									recipe.AddInput(ir.ResourceName, ir.Ratio * curThrottle * elapsed_s);
									if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
									{
										// Input resource amount is zero - stop reactor
										needToStopReactor = true;
									}
								}
								recipe.AddOutput("ElectricCharge", curThrottle * maxECGeneration * elapsed_s, dump: false);
								resources.AddRecipe(recipe);
							}
						}
						// Disable reactor
						if (needToStopReactor)
						{
							Lib.Proto.Set(reactor, "Enabled", false);
							FusionReactorResourceSim.SetProtoCharge(reactor, 0f);
							Lib.Proto.Set(reactor, "Charged", false);
						}
					}
				}
				return brokerTitle;
			}
			return "ERR: no reactor";
		}

		// Find associated Reactor module
		public FusionReactor FindReactorModule(Part part, string moduleName)
		{
			FusionReactor firstReactor = null;
			for (int i = 0; i < part.Modules.Count; i++)
			{
				FusionReactor reactor = part.Modules[i] as FusionReactor;
				if (reactor == null)
					continue;

				if (firstReactor == null)
					firstReactor = reactor;

				if (reactor.ModuleID == moduleName)
				{
					reactorModule = reactor;
					return reactor;
				}
			}

			if (firstReactor != null)
				KFFTUtils.LogError($"[{part}] No FusionReactor named {moduleName} was found, using first instance.");
			else
				KFFTUtils.LogError($"[{part}] No FusionReactor was found.");

			reactorModule = firstReactor;
			return firstReactor;
		}
	}
}

