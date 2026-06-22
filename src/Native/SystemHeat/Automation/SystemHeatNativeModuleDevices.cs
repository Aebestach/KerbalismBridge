using System.Reflection;
using KERBALISM;
using KSP.Localization;
using SystemHeat;

namespace KerbalismNative
{
	internal static class SystemHeatActivatedControl
	{
		internal static void SetActivated(ModuleSystemHeatConverter module, bool value)
		{
			if (module == null || module.IsActivated == value)
				return;

			InvokeToggleMethod(module, value);
			if (module.IsActivated == value)
				return;

			module.IsActivated = value;
		}

		internal static void SetActivated(ModuleSystemHeatHarvester module, bool value)
		{
			if (module == null || module.IsActivated == value)
				return;

			InvokeToggleMethod(module, value);
			if (module.IsActivated == value)
				return;

			module.IsActivated = value;
		}

		private static void InvokeToggleMethod(PartModule module, bool value)
		{
			string methodName = value ? "Activate" : "Deactivate";
			MethodInfo method = module.GetType().GetMethod(
				methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null && method.GetParameters().Length == 0)
				method.Invoke(module, null);
		}

		internal static string StatusText(ModuleSystemHeatConverter module) =>
			Lib.Color(module.IsActivated, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		internal static string StatusText(ModuleSystemHeatHarvester module) =>
			Lib.Color(module.IsActivated, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		internal static string ProtoStatusText(ProtoPartModuleSnapshot protoModule) =>
			Lib.Color(
				Lib.Proto.GetBool(protoModule, "IsActivated"),
				Local.Generic_ON,
				Lib.Kolor.Green,
				Local.Generic_OFF,
				Lib.Kolor.Yellow);
	}

	public sealed class SystemHeatNativeConverterDevice : LoadedDevice<ModuleSystemHeatConverter>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public SystemHeatNativeConverterDevice(ModuleSystemHeatConverter module, string deviceName, string displayName) : base(module)
		{
			this.deviceName = deviceName;
			this.displayName = displayName;
		}

		public override string Name => deviceName;

		public override string DisplayName => displayName;

		public override string Status => SystemHeatActivatedControl.StatusText(module);

		public override void Ctrl(bool value) => SystemHeatActivatedControl.SetActivated(module, value);

		public override void Toggle() => Ctrl(!module.IsActivated);
	}

	public sealed class ProtoSystemHeatNativeConverterDevice : ProtoDevice<ModuleSystemHeatConverter>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public ProtoSystemHeatNativeConverterDevice(
			ModuleSystemHeatConverter prefab,
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

		public override string Status => SystemHeatActivatedControl.ProtoStatusText(protoModule);

		public override void Ctrl(bool value) => Lib.Proto.Set(protoModule, "IsActivated", value);

		public override void Toggle() => Ctrl(!Lib.Proto.GetBool(protoModule, "IsActivated"));
	}

	public sealed class SystemHeatNativeHarvesterDevice : LoadedDevice<ModuleSystemHeatHarvester>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public SystemHeatNativeHarvesterDevice(ModuleSystemHeatHarvester module, string deviceName, string displayName) : base(module)
		{
			this.deviceName = deviceName;
			this.displayName = displayName;
		}

		public override string Name => deviceName;

		public override string DisplayName => displayName;

		public override string Status => SystemHeatActivatedControl.StatusText(module);

		public override void Ctrl(bool value) => SystemHeatActivatedControl.SetActivated(module, value);

		public override void Toggle() => Ctrl(!module.IsActivated);
	}

	public sealed class ProtoSystemHeatNativeHarvesterDevice : ProtoDevice<ModuleSystemHeatHarvester>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public ProtoSystemHeatNativeHarvesterDevice(
			ModuleSystemHeatHarvester prefab,
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

		public override string Status => SystemHeatActivatedControl.ProtoStatusText(protoModule);

		public override void Ctrl(bool value) => Lib.Proto.Set(protoModule, "IsActivated", value);

		public override void Toggle() => Ctrl(!Lib.Proto.GetBool(protoModule, "IsActivated"));
	}
}
