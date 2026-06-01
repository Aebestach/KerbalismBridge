using System.Collections.Generic;
using KERBALISM;
using NearFutureElectrical;

namespace KerbalismNative
{
	internal static class CapacitorDeviceCollector
	{
		internal static void RemoveDevices(List<Device> devices)
		{
			for (int i = devices.Count - 1; i >= 0; i--)
			{
				Device device = devices[i];
				if (device is CapacitorRechargeDevice
					|| device is CapacitorDischargeDevice
					|| device is ProtoCapacitorRechargeDevice
					|| device is ProtoCapacitorDischargeDevice)
				{
					devices.RemoveAt(i);
				}
			}
		}

		internal static void CollectLoaded(Vessel v, List<Device> devices)
		{
			foreach (Part part in v.parts)
			{
				if (part.FindModuleImplementing<NFECapacitorKerbalismUpdater>() == null)
					continue;

				foreach (DischargeCapacitor capacitor in part.GetComponents<DischargeCapacitor>())
				{
					devices.Add(new CapacitorRechargeDevice(capacitor));
					devices.Add(new CapacitorDischargeDevice(capacitor));
				}
			}
		}

		internal static void CollectProto(Vessel v, List<Device> devices)
		{
			var prefabData = new Dictionary<string, Lib.Module_prefab_data>();

			foreach (ProtoPartSnapshot partSnapshot in v.protoVessel.protoPartSnapshots)
			{
				if (NativeUtils.TryFindPartModuleSnapshot(partSnapshot, "NFECapacitorKerbalismUpdater") == null)
					continue;

				Part partPrefab = PartLoader.getPartInfoByName(partSnapshot.partName).partPrefab;
				prefabData.Clear();

				foreach (ProtoPartModuleSnapshot moduleSnapshot in partSnapshot.modules)
				{
					if (moduleSnapshot.moduleName != "DischargeCapacitor")
						continue;

					PartModule modulePrefab = Lib.ModulePrefab(partPrefab.Modules, moduleSnapshot.moduleName, prefabData);
					DischargeCapacitor capacitorPrefab = modulePrefab as DischargeCapacitor;
					if (capacitorPrefab == null)
						continue;

					devices.Add(new ProtoCapacitorRechargeDevice(capacitorPrefab, partSnapshot, moduleSnapshot));
					devices.Add(new ProtoCapacitorDischargeDevice(capacitorPrefab, partSnapshot, moduleSnapshot));
				}
			}
		}
	}
}
