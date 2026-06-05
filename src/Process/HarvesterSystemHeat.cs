using System;
using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;
using SystemHeat;
using KerbalismBridge;

namespace KerbalismProcess
{
    // Heat-only extension of Kerbalism's Harvester (drills / pumps) that emits SystemHeat loop flux.
    // NOTE: Resource IO and rates remain Kerbalism's responsibility.
    public class HarvesterSystemHeat : Harvester, IConfigurable
    {
        private static readonly CrewSpecs EngineerSpecs = new CrewSpecs("Engineer@0");

        // SystemHeat-facing fields; resource IO remains Kerbalism's responsibility.
        [KSPField(isPersistant = false)] public string systemHeatModuleID = "";   // Must match ModuleSystemHeat.moduleID on the same part
        [KSPField(isPersistant = false)] public float shutdownTemperature = 1000f;      // K
        [KSPField(isPersistant = false)] public float systemOutletTemperature = 1000f;  // K
        [KSPField(isPersistant = false)] public float systemPower = 0f;               // kW at full load

        // Efficiency vs loop temperature (mirrors SystemHeat converter behavior)
        [KSPField(isPersistant = false)] public FloatCurve systemEfficiency = new FloatCurve();

        [KSPField(isPersistant = false)] public bool AutoShutdown = true;
        [KSPField(isPersistant = false)] public bool GeneratesHeat = false;

        private ModuleSystemHeat heatModule;

        public void ModuleIsConfigured() { }

        private double lastPlannerThermalEff = -1.0;

        public override string GetInfo()
        {
            string baseInfo = base.GetInfo();
            int pos = baseInfo.IndexOf("\n\n");
            string sh = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_PartInfoAdd",
                          Utils.ToSI(systemPower, "F0"),
                          systemOutletTemperature.ToString("F0"),
                          shutdownTemperature.ToString("F0"));
            return pos < 0 ? baseInfo + "\n\n" + sh : baseInfo.Substring(0, pos) + sh + baseInfo.Substring(pos);
        }

        public void Start()
        {
            heatModule = ModuleUtils.FindHeatModule(part, systemHeatModuleID);
        }

        public void Configure(bool enable, int multiplier)
        {
            if (!enable)
            {
                DisableModule();
                if (heatModule)
                    heatModule.AddFlux(resource, 0f, 0f, false);
            }
        }

        public new void FixedUpdate()
        {
            base.FixedUpdate();

            if (heatModule != null)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    GenerateHeatFlight();
                    UpdateSystemHeatFlight();
                }
                if (HighLogic.LoadedSceneIsEditor)
                {
                    GenerateHeatEditor();
                    RefreshPlannerIfThermalEfficiencyChanged();
                }
            }
        }

        public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
        {
            if (!running || simulated_abundance <= min_abundance)
                return Localizer.Format("#LOC_KerbalismBridge_Brokers_Harvester");

            double thermalEff = GetThermalEfficiencyScale();
            if (ec_rate > double.Epsilon)
                resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -ec_rate * thermalEff));

            List<ProtoCrewMember> crew = GetEditorCrew();
            resourceChangeRequest.Add(new KeyValuePair<string, double>(
                resource,
                Harvester.AdjustedRate(this, EngineerSpecs, crew, simulated_abundance) * thermalEff));

            return Localizer.Format("#LOC_KerbalismBridge_Brokers_Harvester");
        }

        private static List<ProtoCrewMember> GetEditorCrew()
        {
            VesselCrewManifest manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();
            return manifest != null
                ? manifest.GetAllCrew(false).FindAll(k => k != null)
                : new List<ProtoCrewMember>();
        }

        private double GetThermalEfficiencyScale()
        {
            if (heatModule == null)
                return 1.0;

            return SystemHeatEditorSimulation.EvaluateEfficiency(systemEfficiency, heatModule.currentLoopTemperature);
        }

        private void RefreshPlannerIfThermalEfficiencyChanged()
        {
            if (!running)
            {
                lastPlannerThermalEff = -1.0;
                return;
            }

            double thermalEff = GetThermalEfficiencyScale();
            if (Math.Abs(thermalEff - lastPlannerThermalEff) <= SystemHeatEditorSimulation.HystFrac)
                return;

            lastPlannerThermalEff = thermalEff;
            Lib.RefreshPlanner();
        }

        protected void GenerateHeatEditor()
        {
            if (heatModule != null)
            {
                if (ModuleIsActive())
                    heatModule.AddFlux(resource, systemOutletTemperature, systemPower, true);
                else
                    heatModule.AddFlux(resource, 0f, 0f, false);
            }
        }

        protected void GenerateHeatFlight()
        {
            if (ModuleIsActive())
            {
                heatModule.AddFlux(resource, systemOutletTemperature, systemPower, true);
            }
            else
            {
                heatModule.AddFlux(resource, 0f, 0f, false);
            }
        }

        private void UpdateSystemHeatFlight()
        {
            if (!ModuleIsActive())
                return;

            if (AutoShutdown && heatModule.currentLoopTemperature > shutdownTemperature)
            {
                ScreenMessages.PostScreenMessage(
                    new ScreenMessage(
                        Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatHarvester_Message_Shutdown", part.partInfo.title),
                        3.0f, ScreenMessageStyle.UPPER_CENTER));
                base.DisableModule();
            }
        }

        public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
        {
            Harvester.BackgroundUpdate(v, module_snapshot, proto_part_module as Harvester, elapsed_s);
            SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
            return Localizer.Format("#LOC_KerbalismBridge_Brokers_Harvester");
        }
    }
}
