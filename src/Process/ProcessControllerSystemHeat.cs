using System;
using System.Collections.Generic;
using KSP.Localization;
using KERBALISM;
using SystemHeat;
using KerbalismBridge;
using UnityEngine;

namespace KerbalismProcess
{
    // Heat-only extension of Kerbalism's ProcessController that emits SystemHeat loop flux.
    public class ProcessControllerSystemHeat : ProcessController, IConfigurable
    {
        // SystemHeat-facing fields; resource IO remains Kerbalism's responsibility.
        [KSPField(isPersistant = false)] public string systemHeatModuleID = "";   // Must match ModuleSystemHeat.moduleID on the same part
        [KSPField(isPersistant = false)] public float shutdownTemperature = 1000f;      // K; converters and non-fission processes
        [KSPField(isPersistant = false)] public float systemOutletTemperature = 1000f;  // K
        [KSPField(isPersistant = false)] public float systemPower = 0f;               // kW at full load
        [KSPField(isPersistant = true)] public float CurrentPowerPercent = 100f;      // reactor power, 0-100%
        [KSPField(isPersistant = false)] public float MinimumThrottle = 10f;          // minimum non-zero reactor power, %
        [KSPField(isPersistant = false)] public float meltdownTemperature = 0f;       // K; CriticalTemperature; 0 disables core damage
        [KSPField(isPersistant = false)] public float MaximumTemperature = 2000f;   // K; PAW range cap for safety override
        [KSPField(isPersistant = false)] public float CoreDamageRate = 0f;            // %/s/K above meltdownTemperature
        [KSPField(isPersistant = true)] public float CoreDamage = 0f;                 // accumulated core damage, %
        [KSPField(isPersistant = false)] public FloatCurve coreDamageCurve = new FloatCurve();

        // Fission reactor emergency shutdown (mirrors ModuleSystemHeatFissionReactor.CurrentSafetyOverride)
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatFissionReactor_Field_CurrentSafetyOverride", groupName = "fissionreactor", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatFissionReactor_UIGroup_Title"), UI_FloatRange(minValue = 700f, maxValue = 2000f, stepIncrement = 100f)]
        public float CurrentSafetyOverride = 1000f;
        [KSPField(isPersistant = false)] public bool allowManualShutdownTemperatureControl = false;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatFissionReactor_Field_CoreStatus", groupName = "fissionreactor", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatFissionReactor_UIGroup_Title")]
        public string CoreStatus = "100.00 %";

        // Efficiency vs loop temperature (mirrors SystemHeat converter behavior)
        [KSPField(isPersistant = false)] public FloatCurve systemEfficiency = new FloatCurve();

        [KSPField(isPersistant = false)] public bool AutoShutdown = true;
        [KSPField(isPersistant = false)] public bool GeneratesHeat = false;

        // Set by ModuleAnimationGroup on deployable parts (e.g. Sterling preheaters).
        [KSPField(isPersistant = true)] public bool deployed;

        // Must match ModuleAnimationGroup.moduleType for deploy/retract actions (e.g. Preheater, Convector).
        [KSPField(isPersistant = false)] public string deployModuleType = "";

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Efficiency: -1%", groupName = "Process", groupDisplayName = "Process Info")]
        public string ConverterOfEfficiency = "-1%";

        private ModuleSystemHeat heatModule;
        private bool requiresDeploy;

        private double lastAppliedCapacity = -1; // Cache the last capacity we applied so we don't spam writes 

        private double configuredCapacity = -1; // Cache Kerbalism's "100%" capacity after Configure()

        public string ReactorPowerStatus => IsRunning() ? CurrentPowerPercent.ToString("0.#") + "%" : Local.Generic_STOPPED;

        private double ReactorPowerScale => IsRunning() ? Mathf.Clamp(CurrentPowerPercent, 0f, 100f) / 100.0 : 0.0;

        private bool IsFissionReactor() => resource == "_Nukereactor";

        private float EffectiveShutdownTemperature() => IsFissionReactor() ? CurrentSafetyOverride : shutdownTemperature;

        internal bool RequiresDeployGate() => requiresDeploy;

        // Called by Harmony patch on ProcessController.SetRunning (base method is not virtual).
        internal void OnRunningChanged()
        {
            if (DeployGateActive() && !deployed && running)
            {
                base.SetRunning(false);
                if (heatModule != null)
                    heatModule.AddFlux(resource, 0f, 0f, false);
            }

            if (IsRunning() && CurrentPowerPercent <= 0f)
                CurrentPowerPercent = 100f;

            if (!IsRunning())
                SetEfficiencyPlaceholder();

            if (SystemHeatEditorSimulation.IsEditorScene && heatModule != null)
            {
                lastAppliedCapacity = -1;
                if (IsRunning())
                    ApplyThermalCapacityScale(force: true);
                else
                    GenerateHeatEditor();
                KERBALISM.Lib.RefreshPlanner();
            }
        }

        // Editor/tooltip text (shown in part tooltip)
        public override string GetInfo()
        {
            string info = base.GetInfo();
            if (HighLogic.LoadedSceneIsFlight)
                return info;

            float infoShutdown = IsFissionReactor() ? CurrentSafetyOverride : shutdownTemperature;
            string sh = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_PartInfoAdd",
                  Utils.ToSI(systemPower, "F0"),
                  systemOutletTemperature.ToString("F0"),
                  infoShutdown.ToString("F0")
                  );

            int pos = info.IndexOf("\n\n");
            return pos < 0 ? info + sh : info.Substring(0, pos) + sh + info.Substring(pos);
        }

        // Unity lifecycle: Start (no args)
        public new void Start()
        {
            base.Start();

            InitializeDeployState();
            heatModule = ModuleUtils.FindHeatModule(part, systemHeatModuleID);
            if (IsRunning() && CurrentPowerPercent <= 0f)
                CurrentPowerPercent = 100f;

            // Display efficiency for every ProcessControllerSystemHeat on the part (stopped = -1%)
            Fields[nameof(ConverterOfEfficiency)].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Field_Efficiency", title);
            Fields[nameof(ConverterOfEfficiency)].guiActive = true;
            Fields[nameof(ConverterOfEfficiency)].guiActiveEditor = true;
            SetEfficiencyPlaceholder();

            if (SystemHeatEditorSimulation.IsEditorScene && IsRunning())
                SyncPlannerPseudoResource();

            SetupFissionReactorFields();

            if (IsFissionReactor() && (broken || CoreDamage >= 100f))
                ApplyMeltdownState();
        }

        private void InitializeDeployState()
        {
            requiresDeploy = part.FindModuleImplementing<ModuleAnimationGroup>() != null;
            if (!requiresDeploy || Lib.IsEditor())
                deployed = true;
            else
            {
                SyncDeployedFromAnimator();
                if (!deployed && running)
                    base.SetRunning(false);
            }
        }

        private void SyncDeployedFromAnimator()
        {
            ModuleAnimationGroup animator = part.FindModuleImplementing<ModuleAnimationGroup>();
            if (animator == null)
                return;

            bool wasDeployed = deployed;
            deployed = animator.isDeployed;

            if (wasDeployed && !deployed && running)
                base.SetRunning(false);
        }

        internal bool IsDeployedForUse()
        {
            if (!DeployGateActive())
                return true;

            ModuleAnimationGroup animator = part.FindModuleImplementing<ModuleAnimationGroup>();
            return animator == null || animator.isDeployed;
        }

        public override string GetModuleDisplayName()
        {
            if (!string.IsNullOrEmpty(deployModuleType))
                return deployModuleType;
            return base.GetModuleDisplayName();
        }

        private bool DeployGateActive() => requiresDeploy && !Lib.IsEditor();

        public new void EnableModule()
        {
            deployed = true;
        }

        public new void DisableModule()
        {
            deployed = false;
            if (running)
                base.SetRunning(false);

            if (heatModule != null)
                heatModule.AddFlux(resource, 0f, 0f, false);
        }

        public new bool ModuleIsActive()
        {
            return IsDeployedForUse() && !broken && running;
        }

        public new bool IsSituationValid() => true;

        private void SetupFissionReactorFields()
        {
            if (!IsFissionReactor())
            {
                Fields[nameof(CurrentSafetyOverride)].guiActive = false;
                Fields[nameof(CurrentSafetyOverride)].guiActiveEditor = false;
                Fields[nameof(CoreStatus)].guiActive = false;
                return;
            }

            var safetyField = Fields[nameof(CurrentSafetyOverride)];
            safetyField.guiActive = allowManualShutdownTemperatureControl;
            safetyField.guiActiveEditor = allowManualShutdownTemperatureControl;

            var editorRange = (UI_FloatRange)safetyField.uiControlEditor;
            editorRange.minValue = 700f;
            editorRange.maxValue = MaximumTemperature;

            var flightRange = (UI_FloatRange)safetyField.uiControlFlight;
            flightRange.minValue = 700f;
            flightRange.maxValue = MaximumTemperature;

            UpdateCoreStatus();
        }

        private void UpdateCoreStatus()
        {
            if (!IsFissionReactor())
                return;

            if (CoreDamage >= 100f || broken)
            {
                CoreStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionReactor_Field_CoreStatus_Meltdown");
                return;
            }

            float loopK = heatModule != null ? heatModule.currentLoopTemperature : 0f;
            float health = SystemHeatEditorSimulation.GetCoreHealthPercent(
                loopK, meltdownTemperature, MaximumTemperature, CoreDamage);
            CoreStatus = string.Format("{0:F2} %", health);
        }

        /// <summary>
        /// Planner uses sum of _Nukereactor amount (with flow on). In VAB/SPH use full part capacity, not flight bootstrap (0.01) throughput.
        /// </summary>
        internal void SyncPlannerPseudoResource()
        {
            if (!Lib.IsEditor())
                return;

            double fullCapacity = capacity * Math.Max(lastMultiplier, 1);
            if (configuredCapacity <= 0)
                configuredCapacity = fullCapacity;

            if (!IsRunning())
                return;

            Configure(true, Math.Max(lastMultiplier, 1));
            double throttledCapacity = fullCapacity * ReactorPowerScale;
            Lib.SetResource(part, resource, throttledCapacity, throttledCapacity);
            Lib.SetResourceFlow(part, resource, true);
            lastAppliedCapacity = throttledCapacity;
        }

        private void SetEfficiencyPlaceholder()
        {
            ConverterOfEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Field_Efficiency_Value", "-1");
        }

        // ProcessController.Update is not virtual; without this, Toggle/Dump labels stay blank in the PAW.
        public new void Update()
        {
            if (DeployGateActive())
                SyncDeployedFromAnimator();

            // VAB/SPH: PartModule.FixedUpdate often does not run; drive SystemHeat flux from Update instead.
            if (heatModule != null)
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    GenerateHeatEditor();
                    if (IsRunning())
                        ApplyThermalCapacityScale();
                    else
                        SetEfficiencyPlaceholder();
                }
                else if (HighLogic.LoadedSceneIsFlight && !IsRunning())
                    SetEfficiencyPlaceholder();
            }

            if (!KERBALISM.Lib.IsPAWVisible(part))
                return;

            if (DeployGateActive())
                Events["Toggle"].guiActive = IsDeployedForUse() && !broken;
            else
                Events["Toggle"].guiActive = !broken;

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

            if (heatModule == null)
                heatModule = ModuleUtils.FindHeatModule(part, systemHeatModuleID);

            if (!enable)
            {
                SetRunning(false);
                if (heatModule)
                    heatModule.AddFlux(resource, 0f, 0f, false);
            }
            else
                lastAppliedCapacity = -1;
        }

        public void SetReactorPowerPercent(float percent)
        {
            if (percent <= 0f)
            {
                CurrentPowerPercent = 0f;
                SetRunning(false);
            }
            else
            {
                CurrentPowerPercent = Mathf.Clamp(Mathf.Max(percent, MinimumThrottle), 0f, 100f);
                SetRunning(true);
            }

            lastAppliedCapacity = -1;
            if (IsRunning())
                ApplyThermalCapacityScale(force: true);
            else
                SetEfficiencyPlaceholder();
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
                    if (IsRunning())
                        ApplyThermalCapacityScale();
                    else
                        SetEfficiencyPlaceholder();
                }
            }
        }

        protected void GenerateHeatEditor()
        {
            if (heatModule)
            {
                if (IsRunning())
                    heatModule.AddFlux(resource, systemOutletTemperature, (float)(systemPower * lastMultiplier * ReactorPowerScale), true);
                else
                    heatModule.AddFlux(resource, 0f, 0f, false);
            }
        }

        protected void GenerateHeatFlight()
        {
            if (ModuleIsActive())
            {
                float fluxScale = IsRunning() ? (float)ReactorPowerScale : 0f;
                heatModule.AddFlux(resource, systemOutletTemperature, systemPower * fluxScale * lastMultiplier, true);
            }
            else
            {
                heatModule.AddFlux(resource, 0f, 0f, false);
            }
        }
        protected void UpdateSystemHeatFlight()
        {
            if (broken)
                return;

            float loopK = heatModule.currentLoopTemperature;
            ApplyCoreDamage(loopK, TimeWarp.fixedDeltaTime);
            UpdateCoreStatus();
            if (broken)
                return;

            if (AutoShutdown && IsRunning() && loopK > EffectiveShutdownTemperature())
            {
                ScreenMessages.PostScreenMessage(new ScreenMessage(
                    IsFissionReactor()
                        ? Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionReactor_Message_EmergencyShutdown",
                            EffectiveShutdownTemperature().ToString("F0"), part.partInfo.title)
                        : Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Message_Shutdown", part.partInfo.title),
                    IsFissionReactor() ? 5.0f : 3.0f,
                    ScreenMessageStyle.UPPER_CENTER));
                SetRunning(false);
            }
        }
        private void ApplyThermalCapacityScale(bool force = false)
        {
            if (!(HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor) || heatModule == null)
            {
                lastAppliedCapacity = -1;
                return;
            }

            if (!IsRunning())
            {
                lastAppliedCapacity = -1;
                SetEfficiencyPlaceholder();
                return;
            }

            if (configuredCapacity <= 0)
            {
                var pr = part.Resources[resource];
                configuredCapacity = (pr != null && pr.maxAmount > 0) ? pr.maxAmount : Math.Max(capacity, 0.0);
            }

            // Auto-shutdown guard (flight only; editor simulation keeps running to heat the loop)
            float loopK = heatModule.currentLoopTemperature;
            if (AutoShutdown && !SystemHeatEditorSimulation.IsEditorScene && loopK > EffectiveShutdownTemperature())
            {
                if (running)
                {
                    lastAppliedCapacity = -1;
                    SetRunning(false);
                }
                return;
            }

            // VAB/SPH: full throughput for Planner + Process Info (ignore cold loop).
            // Flight: thermal curve + bootstrap only affects Kerbalism IO, not editor planner sync.
            double thermalEff = SystemHeatEditorSimulation.CalculateProcessEfficiency(systemEfficiency, loopK, systemPower, SystemHeatEditorSimulation.IsEditorScene);

            ConverterOfEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Field_Efficiency_Value", (thermalEff * 100f).ToString("F1"));

            double desiredCapacity = SystemHeatEditorSimulation.IsEditorScene
                ? configuredCapacity * ReactorPowerScale
                : configuredCapacity * thermalEff * ReactorPowerScale;

            // Hysteresis to avoid thrash aka only update if a large change has happened
            if (!force && Math.Abs(desiredCapacity - lastAppliedCapacity) <= (configuredCapacity * SystemHeatEditorSimulation.HystFrac))
                return;

            // Reshape Kerbalism's pseudo-resource tank (amount & maxAmount)
            Lib.SetResource(part, resource, desiredCapacity, desiredCapacity);
            Lib.RefreshPlanner();

            lastAppliedCapacity = desiredCapacity;
        }

        private void ApplyCoreDamage(float loopTemperature, float elapsedSeconds)
        {
            if (!IsFissionReactor() || meltdownTemperature <= 0f || elapsedSeconds <= 0f)
                return;

            CoreDamage = SystemHeatEditorSimulation.SyncCoreDamageFromTemperature(
                loopTemperature, meltdownTemperature, MaximumTemperature, CoreDamage);
            if (CoreDamage < 100f)
                return;

            BreakForMeltdown();
        }

        private void BreakForMeltdown()
        {
            ApplyMeltdownState();
            ScreenMessages.PostScreenMessage(new ScreenMessage(
                Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionReactor_Field_ReactorOutput_Meltdown") + " — " + part.partInfo.title,
                5.0f,
                ScreenMessageStyle.UPPER_CENTER));
        }

        private void ApplyMeltdownState()
        {
            CoreDamage = 100f;
            SetRunning(false);
            running = false;
            ReliablityEvent(true);
            broken = true;
            CurrentPowerPercent = 0f;
            isEnabled = false;
            enabled = false;
            UpdateCoreStatus();

            if (heatModule != null)
                heatModule.AddFlux(resource, 0f, 0f, false);

            foreach (Reliability reliability in part.FindModulesImplementing<Reliability>())
            {
                if (!MatchesProcessReliability(reliability))
                    continue;

                reliability.broken = true;
                reliability.critical = true;
            }
        }

        private bool MatchesProcessReliability(Reliability reliability)
        {
            return reliability.type == moduleName
                || reliability.type == nameof(ProcessController)
                || reliability.type == "ProcessController";
        }

        public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
        {
            if (Lib.Proto.GetString(module_snapshot, "resource") == "_Nukereactor")
                SystemHeatBackgroundThermal.SyncFrozenProcessReactor(v, part_snapshot, module_snapshot, proto_part_module, proto_part, elapsed_s);
            else
                SystemHeatBackgroundThermal.TryRun(v, elapsed_s);
            return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_DisplayName");
        }
    }
}
