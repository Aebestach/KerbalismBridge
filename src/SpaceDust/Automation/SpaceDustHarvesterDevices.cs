using KERBALISM;
using KSP.Localization;
using SpaceDust;

namespace KerbalismSpaceDust
{
	internal static class SpaceDustHarvesterControl
	{
		internal static void SetEnabled(ModuleSpaceDustHarvester harvester, bool value)
		{
			if (harvester == null || harvester.Enabled == value)
				return;

			harvester.Enabled = value;
		}

		internal static string StatusText(ModuleSpaceDustHarvester harvester) =>
			Lib.Color(harvester.Enabled, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		internal static string ProtoStatusText(ProtoPartModuleSnapshot protoModule) =>
			Lib.Color(
				Lib.Proto.GetBool(protoModule, "Enabled"),
				Local.Generic_ON,
				Lib.Kolor.Green,
				Local.Generic_OFF,
				Lib.Kolor.Yellow);
	}

	public sealed class SpaceDustHarvesterDevice : LoadedDevice<ModuleSpaceDustHarvester>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public SpaceDustHarvesterDevice(ModuleSpaceDustHarvester module, string deviceName, string displayName) : base(module)
		{
			this.deviceName = deviceName;
			this.displayName = displayName;
		}

		public override string Name => deviceName;

		public override string DisplayName => displayName;

		public override string Status => SpaceDustHarvesterControl.StatusText(module);

		public override void Ctrl(bool value) => SpaceDustHarvesterControl.SetEnabled(module, value);

		public override void Toggle() => Ctrl(!module.Enabled);
	}

	public sealed class ProtoSpaceDustHarvesterDevice : ProtoDevice<ModuleSpaceDustHarvester>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public ProtoSpaceDustHarvesterDevice(
			ModuleSpaceDustHarvester prefab,
			ProtoPartSnapshot protoPart,
			ProtoPartModuleSnapshot protoModule,
			string deviceName,
			string displayName)
			: base(prefab, protoPart, protoModule)
		{
			this.deviceName = deviceName;
			this.displayName = displayName;
		}

		public override string Name => deviceName;

		public override string DisplayName => displayName;

		public override string Status => SpaceDustHarvesterControl.ProtoStatusText(protoModule);

		public override void Ctrl(bool value) => Lib.Proto.Set(protoModule, "Enabled", value);

		public override void Toggle() => Ctrl(!Lib.Proto.GetBool(protoModule, "Enabled"));
	}
}
