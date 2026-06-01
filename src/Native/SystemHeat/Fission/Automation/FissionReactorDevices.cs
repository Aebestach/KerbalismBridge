using System.Reflection;
using KERBALISM;
using KSP.Localization;
using SystemHeat;

namespace KerbalismNative
{
	internal static class FissionReactorControl
	{
		internal static void SetEnabled(ModuleSystemHeatFissionReactor reactor, bool value)
		{
			if (reactor == null || reactor.Enabled == value)
				return;

			string methodName = value ? "EnableReactor" : "DisableReactor";
			MethodInfo method = reactor.GetType().GetMethod(
				methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null && method.GetParameters().Length == 0)
			{
				method.Invoke(reactor, null);
				return;
			}

			reactor.Enabled = value;
		}

		internal static string StatusText(ModuleSystemHeatFissionReactor reactor)
		{
			return Lib.Color(reactor.Enabled, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);
		}

		internal static string ProtoStatusText(ProtoPartModuleSnapshot protoModule)
		{
			return Lib.Color(
				Lib.Proto.GetBool(protoModule, "Enabled"),
				Local.Generic_ON,
				Lib.Kolor.Green,
				Local.Generic_OFF,
				Lib.Kolor.Yellow);
		}
	}

	public sealed class FissionReactorDevice : LoadedDevice<ModuleSystemHeatFissionReactor>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public FissionReactorDevice(ModuleSystemHeatFissionReactor module, string deviceName, string displayName) : base(module)
		{
			this.deviceName = deviceName;
			this.displayName = displayName;
		}

		public override string Name => deviceName;

		public override string DisplayName => displayName;

		public override string Status => FissionReactorControl.StatusText(module);

		public override void Ctrl(bool value)
		{
			FissionReactorControl.SetEnabled(module, value);
		}

		public override void Toggle()
		{
			Ctrl(!module.Enabled);
		}
	}

	public sealed class ProtoFissionReactorDevice : ProtoDevice<ModuleSystemHeatFissionReactor>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public ProtoFissionReactorDevice(
			ModuleSystemHeatFissionReactor prefab,
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

		public override string Status => FissionReactorControl.ProtoStatusText(protoModule);

		public override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "Enabled", value);
		}

		public override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "Enabled"));
		}
	}
}
