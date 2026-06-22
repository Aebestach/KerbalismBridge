using System.Collections.Generic;
using KERBALISM;
using KSP.Localization;
using KerbalismBridge;
using SpaceDust;

namespace KerbalismSpaceDust
{
	internal static class SpaceDustDeviceCollector
	{
		internal static void RemoveDevices(List<Device> devices)
		{
			for (int i = devices.Count - 1; i >= 0; i--)
			{
				Device device = devices[i];
				if (device is SpaceDustHarvesterDevice || device is ProtoSpaceDustHarvesterDevice)
					devices.RemoveAt(i);
			}
		}

		internal static void CollectLoaded(Vessel v, List<Device> devices)
		{
			foreach (Part part in v.parts)
			{
				SpaceDustHarvesterKerbalismUpdater updater = part.FindModuleImplementing<SpaceDustHarvesterKerbalismUpdater>();
				if (updater == null)
					continue;

				ModuleSpaceDustHarvester harvester = FindHarvesterModule(part, updater.harvesterModuleID);
				if (harvester != null)
					AddHarvesterDevice(devices, harvester, updater.harvesterModuleID);
			}
		}

		internal static void CollectProto(Vessel v, List<Device> devices)
		{
			var prefabData = new Dictionary<string, Lib.Module_prefab_data>();

			foreach (ProtoPartSnapshot partSnapshot in v.protoVessel.protoPartSnapshots)
			{
				ProtoPartModuleSnapshot updater = BridgeUtils.TryFindPartModuleSnapshot(partSnapshot, "SpaceDustHarvesterKerbalismUpdater");
				if (updater == null)
					continue;

				string moduleId = Lib.Proto.GetString(updater, "harvesterModuleID");
				TryAddProtoDevice(devices, partSnapshot, prefabData, moduleId);
			}
		}

		private static ModuleSpaceDustHarvester FindHarvesterModule(Part part, string moduleId)
		{
			ModuleSpaceDustHarvester fallback = null;
			for (int i = 0; i < part.Modules.Count; i++)
			{
				ModuleSpaceDustHarvester harvester = part.Modules[i] as ModuleSpaceDustHarvester;
				if (harvester == null)
					continue;

				if (fallback == null)
					fallback = harvester;

				if (string.IsNullOrEmpty(moduleId) || harvester.ModuleID == moduleId)
					return harvester;
			}

			return fallback;
		}

		private static void AddHarvesterDevice(List<Device> devices, ModuleSpaceDustHarvester harvester, string moduleId)
		{
			string resolvedId = harvester.ModuleID;
			if (string.IsNullOrEmpty(resolvedId))
				resolvedId = moduleId;

			string deviceName = Lib.BuildString("spacedust harvester ", resolvedId);
			string displayName = harvester is IModuleInfo info
				? info.GetModuleTitle()
				: Localizer.Format("#LOC_SpaceDust_ModuleSpaceDustHarvester_DisplayName");
			devices.Add(new SpaceDustHarvesterDevice(harvester, deviceName, displayName));
		}

		private static void TryAddProtoDevice(
			List<Device> devices,
			ProtoPartSnapshot partSnapshot,
			Dictionary<string, Lib.Module_prefab_data> prefabData,
			string moduleId)
		{
			ProtoPartModuleSnapshot moduleSnapshot = FindHarvesterSnapshot(partSnapshot, moduleId);
			if (moduleSnapshot == null)
				return;

			Part partPrefab = PartLoader.getPartInfoByName(partSnapshot.partName).partPrefab;
			prefabData.Clear();
			PartModule modulePrefab = Lib.ModulePrefab(partPrefab.Modules, moduleSnapshot.moduleName, prefabData);
			ModuleSpaceDustHarvester harvesterPrefab = modulePrefab as ModuleSpaceDustHarvester;
			if (harvesterPrefab == null)
				return;

			string resolvedId = Lib.Proto.GetString(moduleSnapshot, "ModuleID");
			if (string.IsNullOrEmpty(resolvedId))
				resolvedId = moduleId;

			string deviceName = Lib.BuildString("spacedust harvester ", resolvedId);
			string displayName = harvesterPrefab is IModuleInfo info
				? info.GetModuleTitle()
				: Localizer.Format("#LOC_SpaceDust_ModuleSpaceDustHarvester_DisplayName");
			devices.Add(new ProtoSpaceDustHarvesterDevice(harvesterPrefab, partSnapshot, moduleSnapshot, deviceName, displayName));
		}

		private static ProtoPartModuleSnapshot FindHarvesterSnapshot(ProtoPartSnapshot partSnapshot, string moduleId)
		{
			ProtoPartModuleSnapshot fallback = null;
			for (int i = 0; i < partSnapshot.modules.Count; i++)
			{
				ProtoPartModuleSnapshot moduleSnapshot = partSnapshot.modules[i];
				if (moduleSnapshot.moduleName != "ModuleSpaceDustHarvester")
					continue;

				if (fallback == null)
					fallback = moduleSnapshot;

				if (!string.IsNullOrEmpty(moduleId) && Lib.Proto.GetString(moduleSnapshot, "ModuleID") == moduleId)
					return moduleSnapshot;
			}

			return fallback;
		}
	}
}
