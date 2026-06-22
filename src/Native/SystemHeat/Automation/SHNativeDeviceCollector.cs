using System.Collections.Generic;
using KERBALISM;
using KSP.Localization;
using KerbalismBridge;
using SystemHeat;

namespace KerbalismNative
{
	internal static class SHNativeDeviceCollector
	{
		internal static void RemoveDevices(List<Device> devices)
		{
			for (int i = devices.Count - 1; i >= 0; i--)
			{
				Device device = devices[i];
				if (device is SystemHeatNativeConverterDevice
					|| device is ProtoSystemHeatNativeConverterDevice
					|| device is SystemHeatNativeHarvesterDevice
					|| device is ProtoSystemHeatNativeHarvesterDevice)
					devices.RemoveAt(i);
			}
		}

		internal static void CollectLoaded(Vessel v, List<Device> devices)
		{
			foreach (Part part in v.parts)
			{
				SystemHeatConverterKerbalismUpdater converterUpdater = part.FindModuleImplementing<SystemHeatConverterKerbalismUpdater>();
				if (converterUpdater != null)
				{
					ModuleSystemHeatConverter converter = converterUpdater.FindConverterModule();
					if (converter != null)
						AddConverterDevice(devices, converter, converterUpdater.converterModuleID);
				}

				SystemHeatHarvesterKerbalismUpdater harvesterUpdater = part.FindModuleImplementing<SystemHeatHarvesterKerbalismUpdater>();
				if (harvesterUpdater != null)
				{
					ModuleSystemHeatHarvester harvester = harvesterUpdater.FindHarvesterModule();
					if (harvester != null)
						AddHarvesterDevice(devices, harvester, harvesterUpdater.harvesterModuleID);
				}
			}
		}

		internal static void CollectProto(Vessel v, List<Device> devices)
		{
			var prefabData = new Dictionary<string, Lib.Module_prefab_data>();

			foreach (ProtoPartSnapshot partSnapshot in v.protoVessel.protoPartSnapshots)
			{
				ProtoPartModuleSnapshot converterUpdater = BridgeUtils.TryFindPartModuleSnapshot(partSnapshot, "SystemHeatConverterKerbalismUpdater");
				if (converterUpdater != null)
				{
					string moduleId = Lib.Proto.GetString(converterUpdater, "converterModuleID");
					TryAddConverterProtoDevice(devices, partSnapshot, prefabData, moduleId);
				}

				ProtoPartModuleSnapshot harvesterUpdater = BridgeUtils.TryFindPartModuleSnapshot(partSnapshot, "SystemHeatHarvesterKerbalismUpdater");
				if (harvesterUpdater != null)
				{
					string moduleId = Lib.Proto.GetString(harvesterUpdater, "harvesterModuleID");
					TryAddHarvesterProtoDevice(devices, partSnapshot, prefabData, moduleId);
				}
			}
		}

		private static void AddConverterDevice(List<Device> devices, ModuleSystemHeatConverter converter, string moduleId)
		{
			string resolvedId = converter.moduleID;
			if (string.IsNullOrEmpty(resolvedId))
				resolvedId = moduleId;

			string deviceName = Lib.BuildString("sh converter ", resolvedId);
			string displayName = converter is IModuleInfo info
				? info.GetModuleTitle()
				: Localizer.Format("#LOC_KerbalismBridge_Brokers_Converter");
			devices.Add(new SystemHeatNativeConverterDevice(converter, deviceName, displayName));
		}

		private static void AddHarvesterDevice(List<Device> devices, ModuleSystemHeatHarvester harvester, string moduleId)
		{
			string resolvedId = harvester.moduleID;
			if (string.IsNullOrEmpty(resolvedId))
				resolvedId = moduleId;

			string deviceName = Lib.BuildString("sh harvester ", resolvedId);
			string displayName = harvester is IModuleInfo info
				? info.GetModuleTitle()
				: Localizer.Format("#LOC_KerbalismBridge_Brokers_Harvester");
			devices.Add(new SystemHeatNativeHarvesterDevice(harvester, deviceName, displayName));
		}

		private static void TryAddConverterProtoDevice(
			List<Device> devices,
			ProtoPartSnapshot partSnapshot,
			Dictionary<string, Lib.Module_prefab_data> prefabData,
			string moduleId)
		{
			ProtoPartModuleSnapshot moduleSnapshot = FindModuleSnapshot(partSnapshot, "ModuleSystemHeatConverter", "moduleID", moduleId);
			if (moduleSnapshot == null)
				return;

			Part partPrefab = PartLoader.getPartInfoByName(partSnapshot.partName).partPrefab;
			prefabData.Clear();
			PartModule modulePrefab = Lib.ModulePrefab(partPrefab.Modules, moduleSnapshot.moduleName, prefabData);
			ModuleSystemHeatConverter converterPrefab = modulePrefab as ModuleSystemHeatConverter;
			if (converterPrefab == null)
				return;

			string resolvedId = Lib.Proto.GetString(moduleSnapshot, "moduleID");
			if (string.IsNullOrEmpty(resolvedId))
				resolvedId = moduleId;

			string deviceName = Lib.BuildString("sh converter ", resolvedId);
			string displayName = converterPrefab is IModuleInfo info
				? info.GetModuleTitle()
				: Localizer.Format("#LOC_KerbalismBridge_Brokers_Converter");
			devices.Add(new ProtoSystemHeatNativeConverterDevice(converterPrefab, partSnapshot, moduleSnapshot, deviceName, displayName));
		}

		private static void TryAddHarvesterProtoDevice(
			List<Device> devices,
			ProtoPartSnapshot partSnapshot,
			Dictionary<string, Lib.Module_prefab_data> prefabData,
			string moduleId)
		{
			ProtoPartModuleSnapshot moduleSnapshot = FindModuleSnapshot(partSnapshot, "ModuleSystemHeatHarvester", "moduleID", moduleId);
			if (moduleSnapshot == null)
				return;

			Part partPrefab = PartLoader.getPartInfoByName(partSnapshot.partName).partPrefab;
			prefabData.Clear();
			PartModule modulePrefab = Lib.ModulePrefab(partPrefab.Modules, moduleSnapshot.moduleName, prefabData);
			ModuleSystemHeatHarvester harvesterPrefab = modulePrefab as ModuleSystemHeatHarvester;
			if (harvesterPrefab == null)
				return;

			string resolvedId = Lib.Proto.GetString(moduleSnapshot, "moduleID");
			if (string.IsNullOrEmpty(resolvedId))
				resolvedId = moduleId;

			string deviceName = Lib.BuildString("sh harvester ", resolvedId);
			string displayName = harvesterPrefab is IModuleInfo info
				? info.GetModuleTitle()
				: Localizer.Format("#LOC_KerbalismBridge_Brokers_Harvester");
			devices.Add(new ProtoSystemHeatNativeHarvesterDevice(harvesterPrefab, partSnapshot, moduleSnapshot, deviceName, displayName));
		}

		private static ProtoPartModuleSnapshot FindModuleSnapshot(
			ProtoPartSnapshot partSnapshot,
			string moduleName,
			string idFieldName,
			string moduleId)
		{
			ProtoPartModuleSnapshot fallback = null;
			for (int i = 0; i < partSnapshot.modules.Count; i++)
			{
				ProtoPartModuleSnapshot moduleSnapshot = partSnapshot.modules[i];
				if (moduleSnapshot.moduleName != moduleName)
					continue;

				if (fallback == null)
					fallback = moduleSnapshot;

				if (!string.IsNullOrEmpty(moduleId) && Lib.Proto.GetString(moduleSnapshot, idFieldName) == moduleId)
					return moduleSnapshot;
			}

			return fallback;
		}
	}
}
