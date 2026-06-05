using KERBALISM;
using KSP.Localization;

namespace KerbalismProcess
{
	/// <summary>
	/// Automation devices for NFE Layer A fission reactors (ProcessControllerSystemHeat + _Nukereactor).
	/// Uses the same display name as native SystemHeat fission reactor devices.
	/// </summary>
	public sealed class FissionReactorProcessDevice : LoadedDevice<ProcessControllerSystemHeat>
	{
		public FissionReactorProcessDevice(ProcessControllerSystemHeat module) : base(module) { }

		public override bool IsVisible => module.toggle;

		public override string DisplayName => Localizer.Format("#LOC_KerbalismBridge_Device_FissionReactor");

		public override string Tooltip => Lib.BuildString(base.Tooltip, "\n", Lib.Bold("Process capacity :"), "\n", module.ModuleInfo);

		public override string Status => Lib.Color(module.IsRunning(), Local.Generic_RUNNING, Lib.Kolor.Green, Local.Generic_STOPPED, Lib.Kolor.Yellow);

		public override void Ctrl(bool value) => module.SetReactorPowerPercent(value ? 100f : 0f);

		public override void Toggle() => Ctrl(!module.IsRunning());
	}

	public sealed class ProtoFissionReactorProcessDevice : ProtoDevice<ProcessControllerSystemHeat>
	{
		public ProtoFissionReactorProcessDevice(ProcessControllerSystemHeat prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		public override bool IsVisible => prefab.toggle;

		public override string DisplayName => Localizer.Format("#LOC_KerbalismBridge_Device_FissionReactor");

		public override string Tooltip => Lib.BuildString(base.Tooltip, "\n", Lib.Bold("Process capacity :"), "\n", prefab.ModuleInfo);

		public override string Status => Lib.Color(Lib.Proto.GetBool(protoModule, nameof(ProcessController.running)), Local.Generic_RUNNING, Lib.Kolor.Green, Local.Generic_STOPPED, Lib.Kolor.Yellow);

		public override void Ctrl(bool value)
		{
			if (Lib.Proto.GetBool(protoModule, nameof(ProcessController.broken)))
				return;

			Lib.Proto.Set(protoModule, nameof(ProcessController.running), value);
			Lib.Proto.Set(protoModule, nameof(ProcessControllerSystemHeat.CurrentPowerPercent), value ? 100f : 0f);
			var res = protoPart.resources.Find(k => k.resourceName == prefab.resource);
			if (res != null) res.flowState = value;
		}

		public override void Toggle() => Ctrl(!Lib.Proto.GetBool(protoModule, nameof(ProcessController.running)));
	}
}
