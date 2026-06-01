using System;
using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;
using SystemHeat;
using KerbalismBridge;

namespace KerbalismProcess
{
    // Heat-only extension of Kerbalism's ProcessController that emits SystemHeat loop flux.
    public class ProcessControllerSystemHeat : ProcessController, IConfigurable
    {
        // --- SystemHeat-facing fields (no resource IO here) ---
        [KSPField(isPersistant = false)] public string systemHeatModuleID = "";   // Must match ModuleSystemHeat.moduleID on the same part
        [KSPField(isPersistant = false)] public float shutdownTemperature = 1000f;      // K
        [KSPField(isPersistant = false)] public float systemOutletTemperature = 1000f;  // K
        [KSPField(isPersistant = false)] public float systemPower = 0f;               // kW at full load

        // Efficiency vs loop temperature (mirrors SystemHeat converter behavior)
        [KSPField(isPersistant = false)] public FloatCurve systemEfficiency = new FloatCurve();

        [KSPField(isPersistant = false)] public bool AutoShutdown = true;
        [KSPField(isPersistant = false)] public bool GeneratesHeat = false;

        // Current efficiency GUI string
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Efficiency: -1%", groupName = "Process", groupDisplayName = "Process Info")]
        public string ConverterOfEfficiency = "-1%";

        // Cached SystemHeat module on this part
        private ModuleSystemHeat heatModule;  // ModuleSystemHeat

        private double lastAppliedCapacity = -1; // Cache the last capacity we applied so we don't spam writes 

        private double configuredCapacity = -1; // Cache Kerbalism's "100%" capacity after Configure()

        // Tunables
        private const double HYST_FRAC = SystemHeatEditorSimulation.HystFrac;

        // Called by Harmony patch on ProcessController.SetRunning (base method is not virtual).
        internal void OnRunningChanged()
        {
            if (SystemHeatEditorSimulation.IsEditorScene && heatModule != null)
            {
                lastAppliedCapacity = -1;
                if (IsRunning())
                    ApplyThermalCapacityScale(force: true);
                KERBALISM.Lib.RefreshPlanner();
            }
        }

        // Editor/tooltip text (shown in part tooltip)
        public override string GetInfo()
        {
            string info = base.GetInfo();

            int pos = info.IndexOf("\n\n");
            if (pos < 0)
                return info;
            else
                return info.Substring(0, pos) + Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_PartInfoAdd",
                  Utils.ToSI(systemPower, "F0"),
                  systemOutletTemperature.ToString("F0"),
                  shutdownTemperature.ToString("F0")
                  ) + info.Substring(pos);
        }

        // Unity lifecycle: Start (no args)
        public new void Start()
        {
            base.Start();

            // Find ModuleSystemHeat with matching moduleID
            heatModule = ModuleUtils.FindHeatModule(this.part, systemHeatModuleID);

            //Display Efficiency on the UI
            Fields[nameof(ConverterOfEfficiency)].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Field_Efficiency", title);
        }

        // ProcessController.Update is not virtual; without this, Toggle/Dump labels stay blank in the PAW.
        public new void Update()
        {
            if (!KERBALISM.Lib.IsPAWVisible(part))
                return;

            Events["Toggle"].guiName = KERBALISM.Lib.StatusToggle(lastMultiplier + " " + title,
                broken ? KERBALISM.Local.ProcessController_broken
                    : running ? KERBALISM.Local.ProcessController_running
                    : KERBALISM.Local.ProcessController_stopped);

            if (Events["DumpValve"].active)
                Events["DumpValve"].guiName = KERBALISM.Local.ProcessController_Dump;
        }

        public override void Configure(bool enable, int multiplier)
        {
            configuredCapacity = capacity * multiplier;
            base.Configure(enable, multiplier);

            if (!enable)
            {
                SetRunning(false);
                if (heatModule)
                    heatModule.AddFlux(resource, 0f, 0f, false);
            }
        }

        // KSP FixedUpdate for flight-time heat emission
        public void FixedUpdate()
        {
            if (heatModule != null)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    GenerateHeatFlight();
                    UpdateSystemHeatFlight();

                    Fields[nameof(ConverterOfEfficiency)].guiActive = base.ModuleIsActive();
                }
                if (HighLogic.LoadedSceneIsEditor)
                {
                    GenerateHeatEditor();

                    Fields[nameof(ConverterOfEfficiency)].guiActiveEditor = IsRunning();
                }

                ApplyThermalCapacityScale();
            }
        }

        protected void GenerateHeatEditor()
        {
            if (heatModule)
            {
                if (IsRunning())
                    heatModule.AddFlux(resource, systemOutletTemperature, systemPower * lastMultiplier, true);
                else
                    heatModule.AddFlux(resource, 0f, 0f, false);
            }
        }

        protected void GenerateHeatFlight()
        {            
            if (ModuleIsActive())
            {
                float fluxScale = 1f;
                if (base.IsRunning() == false)
                {
                    fluxScale = 0f;
                }
                heatModule.AddFlux(resource, systemOutletTemperature, systemPower * fluxScale * lastMultiplier, true);
            }
            else
            {
                heatModule.AddFlux(resource, 0f, 0f, false);
            }
        }
        protected void UpdateSystemHeatFlight()
        {
            if (ModuleIsActive())
            {
                if (heatModule.currentLoopTemperature > shutdownTemperature)
                {
                    ScreenMessages.PostScreenMessage(
                      new ScreenMessage(
                        Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Message_Shutdown",
                                                                       part.partInfo.title),
                                                                       3.0f,
                                                                       ScreenMessageStyle.UPPER_CENTER));
                    SetRunning(false); //shut down the process
                }
            }
        }
        private void ApplyThermalCapacityScale(bool force = false)
        {
            //Make sure we are in flight or in editor, Make sure we are running, and make we have the heat module if not exit
            if (!(HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor) || !IsRunning() || heatModule == null)
            {
                lastAppliedCapacity = -1; //reset systemheat calculations
                return;
            }

            // Make sure we know the base (100%) capacity
            if (configuredCapacity <= 0)
            {
                // Prefer the ProcessController's configured capacity if you expose it;
                // fallback to the current pseudo-resource tank size as "base".
                var pr = part.Resources[resource];
                configuredCapacity = (pr != null && pr.maxAmount > 0) ? pr.maxAmount : Math.Max(capacity, 0.0);
            }

            // Auto-shutdown guard (flight only; editor simulation keeps running to heat the loop)
            float loopK = heatModule.currentLoopTemperature;
            if (AutoShutdown && !SystemHeatEditorSimulation.IsEditorScene && loopK >= shutdownTemperature)
            {
                if (running)
                {
                    lastAppliedCapacity = -1; //reset systemheat calculations
                    SetRunning(false);
                }
                return;
            }

            // Thermal efficiency from curve is not null if it is set to 100%
            double thermalEff = SystemHeatEditorSimulation.EvaluateEfficiency(systemEfficiency, loopK);

            //update UI with values we use here to make sure things are consistent
            ConverterOfEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Field_Efficiency_Value", (thermalEff * 100f).ToString("F1"));

            // Target capacity Kerbalism should see now
            double desiredCapacity = configuredCapacity * thermalEff;

            // Hysteresis to avoid thrash aka only update if a large change has happened
            if (!force && Math.Abs(desiredCapacity - lastAppliedCapacity) <= (configuredCapacity * HYST_FRAC))
                return;

            // Reshape Kerbalism's pseudo-resource tank (amount & maxAmount)
            Lib.SetResource(part, resource, desiredCapacity, desiredCapacity);
            Lib.RefreshPlanner();

            lastAppliedCapacity = desiredCapacity;
        }

        public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
        {
            SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
            return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_DisplayName");
        }
    }
}
