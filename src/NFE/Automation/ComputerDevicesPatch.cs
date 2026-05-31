using System.Collections.Generic;
using HarmonyLib;
using KERBALISM;
using NearFutureElectrical;

namespace KerbalismNFE
{
	[HarmonyPatch(typeof(Computer), nameof(Computer.GetModuleDevices))]
	internal static class Patch_Computer_GetModuleDevices
	{
		private static void Postfix(Vessel v, ref List<Device> __result)
		{
			if (__result == null || v == null || !Features.Automation)
				return;

			RemoveCapacitorDevices(__result);

			var capacitorDevices = new List<Device>();
			if (v.loaded)
				CollectLoadedDevices(v, capacitorDevices);
			else
				CollectProtoDevices(v, capacitorDevices);

			if (capacitorDevices.Count == 0)
				return;

			// Insert before vessel-wide devices so DevManager section headers stay correct.
			__result.InsertRange(FindFirstVesselDeviceIndex(__result), capacitorDevices);
		}

		private static int FindFirstVesselDeviceIndex(List<Device> devices)
		{
			for (int i = 0; i < devices.Count; i++)
			{
				if (devices[i] is VesselDevice)
					return i;
			}

			return devices.Count;
		}

		private static void RemoveCapacitorDevices(List<Device> devices)
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

		private static void CollectLoadedDevices(Vessel v, List<Device> devices)
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

		private static void CollectProtoDevices(Vessel v, List<Device> devices)
		{
			var prefabData = new Dictionary<string, Lib.Module_prefab_data>();

			foreach (ProtoPartSnapshot partSnapshot in v.protoVessel.protoPartSnapshots)
			{
				if (KNFEUtils.TryFindPartModuleSnapshot(partSnapshot, "NFECapacitorKerbalismUpdater") == null)
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
