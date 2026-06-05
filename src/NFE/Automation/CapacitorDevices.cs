using KERBALISM;
using KSP.Localization;
using NearFutureElectrical;

namespace KerbalismNFE
{
	public sealed class CapacitorRechargeDevice : LoadedDevice<DischargeCapacitor>
	{
		public CapacitorRechargeDevice(DischargeCapacitor module) : base(module) { }

		public override string Name => "NFE capacitor recharge";

		public override string DisplayName => Localizer.Format("#LOC_KerbalismNFE_Device_CapacitorRecharge");

		public override string Status => Lib.Color(module.Enabled, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		public override void Ctrl(bool value)
		{
			if (value)
				module.Enable();
			else
				module.Disable();
		}

		public override void Toggle()
		{
			Ctrl(!module.Enabled);
		}
	}

	public sealed class CapacitorDischargeDevice : LoadedDevice<DischargeCapacitor>
	{
		public CapacitorDischargeDevice(DischargeCapacitor module) : base(module) { }

		public override string Name => "NFE capacitor discharge";

		public override string DisplayName => Localizer.Format("#LOC_KerbalismNFE_Device_CapacitorDischarge");

		public override string Status => Lib.Color(module.Discharging, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		public override void Ctrl(bool value)
		{
			if (value)
				module.Discharge();
			else
				module.Discharging = false;
		}

		public override void Toggle()
		{
			Ctrl(!module.Discharging);
		}
	}

	public sealed class ProtoCapacitorRechargeDevice : ProtoDevice<DischargeCapacitor>
	{
		public ProtoCapacitorRechargeDevice(DischargeCapacitor prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		public override string Name => "NFE capacitor recharge";

		public override string DisplayName => Localizer.Format("#LOC_KerbalismNFE_Device_CapacitorRecharge");

		public override string Status => Lib.Color(Lib.Proto.GetBool(protoModule, "Enabled"), Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		public override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "Enabled", value);
		}

		public override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "Enabled"));
		}
	}

	public sealed class ProtoCapacitorDischargeDevice : ProtoDevice<DischargeCapacitor>
	{
		public ProtoCapacitorDischargeDevice(DischargeCapacitor prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		public override string Name => "NFE capacitor discharge";

		public override string DisplayName => Localizer.Format("#LOC_KerbalismNFE_Device_CapacitorDischarge");

		public override string Status => Lib.Color(Lib.Proto.GetBool(protoModule, "Discharging"), Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		public override void Ctrl(bool value)
		{
			if (value && GetStoredCharge(protoPart) <= 1e-6)
				return;

			Lib.Proto.Set(protoModule, "Discharging", value);
		}

		public override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "Discharging"));
		}

		private static double GetStoredCharge(ProtoPartSnapshot partSnapshot)
		{
			for (int i = 0; i < partSnapshot.resources.Count; i++)
			{
				if (partSnapshot.resources[i].resourceName == "StoredCharge")
					return partSnapshot.resources[i].amount;
			}
			return 0.0;
		}
	}
}
