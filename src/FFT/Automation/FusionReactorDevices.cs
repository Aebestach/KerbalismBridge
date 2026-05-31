using System.Reflection;
using FarFutureTechnologies;
using KERBALISM;
using KSP.Localization;

namespace KerbalismFFT
{
	internal static class FusionReactorControl
	{
		internal static void SetEnabled(FusionReactor reactor, bool value)
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
			if (!value)
			{
				reactor.Charging = false;
				reactor.Charged = false;
				reactor.CurrentCharge = 0f;
			}
		}

		internal static string StatusText(FusionReactor reactor)
		{
			if (!reactor.Enabled && reactor.Charging && !reactor.Charged)
				return Lib.Color(false, Localizer.Format("#LOC_KerbalismFFT_Device_FusionReactor_Charging"), Lib.Kolor.Yellow, Local.Generic_OFF, Lib.Kolor.Yellow);

			return Lib.Color(reactor.Enabled, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);
		}

		internal static string ProtoStatusText(ProtoPartModuleSnapshot protoModule)
		{
			bool enabled = Lib.Proto.GetBool(protoModule, "Enabled");
			bool charging = Lib.Proto.GetBool(protoModule, "Charging");
			bool charged = Lib.Proto.GetBool(protoModule, "Charged");
			if (!enabled && charging && !charged)
				return Lib.Color(false, Localizer.Format("#LOC_KerbalismFFT_Device_FusionReactor_Charging"), Lib.Kolor.Yellow, Local.Generic_OFF, Lib.Kolor.Yellow);

			return Lib.Color(enabled, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);
		}
	}

	public sealed class FusionReactorDevice : LoadedDevice<FusionReactor>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public FusionReactorDevice(FusionReactor module, string deviceName, string displayName) : base(module)
		{
			this.deviceName = deviceName;
			this.displayName = displayName;
		}

		public override string Name => deviceName;

		public override string DisplayName => displayName;

		public override string Status => FusionReactorControl.StatusText(module);

		public override void Ctrl(bool value)
		{
			FusionReactorControl.SetEnabled(module, value);
		}

		public override void Toggle()
		{
			Ctrl(!module.Enabled);
		}
	}

	public sealed class ProtoFusionReactorDevice : ProtoDevice<FusionReactor>
	{
		private readonly string deviceName;
		private readonly string displayName;

		public ProtoFusionReactorDevice(FusionReactor prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule, string deviceName, string displayName)
			: base(prefab, protoPart, protoModule)
		{
			this.deviceName = deviceName;
			this.displayName = displayName;
		}

		public override string Name => deviceName;

		public override string DisplayName => displayName;

		public override string Status => FusionReactorControl.ProtoStatusText(protoModule);

		public override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "Enabled", value);
			if (!value)
			{
				Lib.Proto.Set(protoModule, "Charging", false);
				Lib.Proto.Set(protoModule, "Charged", false);
				Lib.Proto.Set(protoModule, "CurrentCharge", 0f);
			}
		}

		public override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "Enabled"));
		}
	}
}
