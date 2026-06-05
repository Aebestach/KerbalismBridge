using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using KERBALISM;
using SystemHeat;
using KerbalismBridge;

namespace KerbalismNative
{
	public class SystemHeatFissionEngineKerbalismUpdater : PartModule, IKerbalismModule
	{
		public static string brokerName = "SHFissionEngine";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismBridge_Brokers_FissionEngine");

		[KSPField(isPersistant = true)]
		public bool FirstLoad = true;

		[KSPField(isPersistant = true)]
		public string engineModuleID;

		[KSPField(isPersistant = true)]
		public float MaxECGeneration = 0f;
		[KSPField(isPersistant = true)]
		public float MinThrottle = 0.25f;
		[KSPField(isPersistant = true)]
		public float MaxThrottle = 1.0f;
		[KSPField(isPersistant = true)]
		public bool GeneratesElectricity = true;

		protected static string engineModuleName = "ModuleSystemHeatFissionEngine";
		protected ModuleSystemHeatFissionReactor engineModule;

		protected bool resourcesListParsed = false;
		protected List<ResourceRatio> inputs;
		protected List<ResourceRatio> outputs;

		internal ModuleSystemHeatFissionReactor EngineModule => engineModule;
		internal List<ResourceRatio> Inputs => inputs;
		internal List<ResourceRatio> Outputs => outputs;

		internal void EnsureResourcesParsed()
		{
			if (!resourcesListParsed)
				ParseResourcesList(part);
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
						MaxECGeneration = (float)engineModule.ElectricalGeneration.Evaluate(100f);
						MinThrottle = engineModule.MinimumThrottle / 100f;
						GeneratesElectricity = engineModule.GeneratesElectricity;
					}
					EnsureResourcesParsed();
					FirstLoad = false;
				}
			}
		}

		public virtual void FixedUpdate()
		{
			if (engineModule != null && Lib.IsFlight())
			{
				MaxThrottle = engineModule.CoreIntegrity / 100f;
				if (MinThrottle > MaxThrottle)
					MinThrottle = MaxThrottle;

				FissionReactorResourceSim.UpdateAutoThrottle(engineModule, TimeWarp.fixedDeltaTime);
				EnsureResourcesParsed();
				FissionReactorResourceSim.ValidateLoadedReactor(
					engineModule,
					vessel,
					inputs,
					outputs,
					brokerTitle,
					part.partInfo.title);
			}
		}

		protected void ParseResourcesList(Part part)
		{
			if (resourcesListParsed)
				return;

			ConfigNode node = ModuleUtils.GetModuleConfigNode(part, engineModuleName);
			if (node != null)
			{
				inputs = new List<ResourceRatio>();
				foreach (ConfigNode inNode in node.GetNodes("INPUT_RESOURCE"))
				{
					ResourceRatio p = new ResourceRatio();
					p.Load(inNode);
					inputs.Add(p);
				}

				outputs = new List<ResourceRatio>();
				foreach (ConfigNode outNode in node.GetNodes("OUTPUT_RESOURCE"))
				{
					ResourceRatio p = new ResourceRatio();
					p.Load(outNode);
					outputs.Add(p);
				}
			}
			resourcesListParsed = true;
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (engineModule != null)
			{
				float curECGeneration = (float)engineModule.ElectricalGeneration.Evaluate(engineModule.CurrentReactorThrottle);
				if (curECGeneration > 0)
					resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", curECGeneration));

				float fuelThrottle = engineModule.CurrentReactorThrottle / 100f;
				if (fuelThrottle > 0)
				{
					EnsureResourcesParsed();
					foreach (ResourceRatio ratio in inputs)
						resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, -fuelThrottle * ratio.Ratio));
					foreach (ResourceRatio ratio in outputs)
						resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, fuelThrottle * ratio.Ratio));
				}
				return brokerTitle;
			}
			return "ERR: no engine";
		}

		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (engineModule == null)
				engineModule = FindEngineModule(part, engineModuleID);
			return FissionReactorResourceSim.AddLoadedRates(this, availableResources, resourceChangeRequest);
		}

		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			ProtoPartModuleSnapshot reactor = BridgeUtils.FindPartModuleSnapshot(part_snapshot, engineModuleName);
			if (reactor != null)
			{
				if (Lib.Proto.GetBool(reactor, "Enabled") && Lib.Proto.GetBool(module_snapshot, "GeneratesElectricity"))
				{
					float curThrottle = Lib.Proto.GetFloat(reactor, "CurrentReactorThrottle") / 100f;
					float minThrottle = Lib.Proto.GetFloat(module_snapshot, "MinThrottle");
					float maxThrottle = Lib.Proto.GetFloat(module_snapshot, "MaxThrottle");
					float maxECGeneration = Lib.Proto.GetFloat(module_snapshot, "MaxECGeneration");
					bool needToStopReactor = false;
					if (maxECGeneration > 0)
					{
						VesselResources resources = KERBALISM.ResourceCache.Get(v);
						var updater = proto_part_module as SystemHeatFissionEngineKerbalismUpdater;
						if (!updater.resourcesListParsed)
							updater.ParseResourcesList(proto_part);

						if (minThrottle > 0)
						{
							ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
								brokerName,
								KERBALISM.ResourceBroker.BrokerCategory.Converter,
								brokerTitle));
							foreach (ResourceRatio ir in updater.inputs)
							{
								recipe.AddInput(ir.ResourceName, ir.Ratio * minThrottle * elapsed_s);
								if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
									needToStopReactor = true;
							}
							foreach (ResourceRatio or in updater.outputs)
							{
								recipe.AddOutput(or.ResourceName, or.Ratio * minThrottle * elapsed_s, dump: false);
								if (1 - resources.GetResource(v, or.ResourceName).Level < double.Epsilon)
								{
									needToStopReactor = true;
									Message.Post(
										Severity.warning,
										Localizer.Format(
											"#LOC_KerbalismBridge_ReactorOutputResourceFull",
											or.ResourceName,
											v.GetDisplayName(),
											part_snapshot.partName)
									);
								}
							}
							recipe.AddOutput("ElectricCharge", minThrottle * maxECGeneration * elapsed_s, dump: true);
							resources.AddRecipe(recipe);
						}

						if (!needToStopReactor)
						{
							if (!Lib.Proto.GetBool(reactor, "ManualControl"))
								curThrottle = maxThrottle;
							curThrottle -= minThrottle;
							if (curThrottle > 0)
							{
								ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
									brokerName,
									KERBALISM.ResourceBroker.BrokerCategory.Converter,
									brokerTitle));
								foreach (ResourceRatio ir in updater.inputs)
								{
									recipe.AddInput(ir.ResourceName, ir.Ratio * curThrottle * elapsed_s);
									if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
										needToStopReactor = true;
								}
								foreach (ResourceRatio or in updater.outputs)
								{
									recipe.AddOutput(or.ResourceName, or.Ratio * curThrottle * elapsed_s, dump: false);
									if (1 - resources.GetResource(v, or.ResourceName).Level < double.Epsilon)
									{
										needToStopReactor = true;
										Message.Post(
											Severity.warning,
											Localizer.Format(
												"#LOC_KerbalismBridge_ReactorOutputResourceFull",
												or.ResourceName,
												v.GetDisplayName(),
												part_snapshot.partName)
										);
									}
								}
								recipe.AddOutput("ElectricCharge", curThrottle * maxECGeneration * elapsed_s, dump: false);
								resources.AddRecipe(recipe);
							}
						}
					}

					if (needToStopReactor)
						Lib.Proto.Set(reactor, "Enabled", false);
				}
				Lib.Proto.Set(reactor, "LastUpdateTime", Planetarium.GetUniversalTime());
			}

			SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
			return brokerTitle;
		}

		public ModuleSystemHeatFissionEngine FindEngineModule(Part part, string moduleName)
		{
			ModuleSystemHeatFissionEngine engine = part.GetComponents<ModuleSystemHeatFissionEngine>().ToList().Find(x => x.moduleID == moduleName);

			if (engine == null)
			{
				BridgeUtils.LogError($"[{part}] No ModuleSystemHeatFissionEngine named {moduleName} was found, using first instance");
				engine = part.GetComponents<ModuleSystemHeatFissionEngine>().ToList().FirstOrDefault();
			}
			if (engine == null)
				BridgeUtils.LogError($"[{part}] No ModuleSystemHeatFissionEngine was found.");
			engineModule = engine;
			return engine;
		}
	}
}
