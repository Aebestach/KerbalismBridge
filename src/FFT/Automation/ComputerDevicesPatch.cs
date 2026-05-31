using System.Collections.Generic;
using FarFutureTechnologies;
using KERBALISM;
using KSP.Localization;

namespace KerbalismFFT
{
	internal static class Patch_Computer_GetModuleDevices
	{
		internal static void Postfix(Vessel v, ref List<Device> __result)
		{
			if (__result == null || v == null || !Features.Automation)
				return;

			RemoveFusionReactorDevices(__result);

			var fusionDevices = new List<Device>();
			if (v.loaded)
				CollectLoadedDevices(v, fusionDevices);
			else
				CollectProtoDevices(v, fusionDevices);

			if (fusionDevices.Count == 0)
				return;

			__result.InsertRange(FindFirstVesselDeviceIndex(__result), fusionDevices);
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

		private static void RemoveFusionReactorDevices(List<Device> devices)
		{
			for (int i = devices.Count - 1; i >= 0; i--)
			{
				Device device = devices[i];
				if (device is FusionReactorDevice || device is ProtoFusionReactorDevice)
					devices.RemoveAt(i);
			}
		}

		private static void CollectLoadedDevices(Vessel v, List<Device> devices)
		{
			foreach (Part part in v.parts)
			{
				FFTFusionEngineKerbalismUpdater engineUpdater = part.FindModuleImplementing<FFTFusionEngineKerbalismUpdater>();
				if (engineUpdater != null)
				{
					ModuleFusionEngine engine = engineUpdater.FindEngineModule(part, engineUpdater.engineModuleID);
					if (engine != null)
						AddLoadedDevice(devices, engine, true);
					continue;
				}

				FFTFusionReactorKerbalismUpdater reactorUpdater = part.FindModuleImplementing<FFTFusionReactorKerbalismUpdater>();
				if (reactorUpdater == null)
					continue;

				FusionReactor reactor = reactorUpdater.FindReactorModule(part, reactorUpdater.reactorModuleID);
				if (reactor != null)
					AddLoadedDevice(devices, reactor, false);
			}
		}

		private static void AddLoadedDevice(List<Device> devices, FusionReactor reactor, bool isEngine)
		{
			string moduleId = reactor.ModuleID;
			string deviceName = isEngine
				? "FFT fusion engine reactor " + moduleId
				: "FFT fusion reactor " + moduleId;
			string displayName = isEngine
				? Localizer.Format("#LOC_KerbalismFFT_Device_FusionEngineReactor")
				: Localizer.Format("#LOC_KerbalismFFT_Device_FusionReactor");

			devices.Add(new FusionReactorDevice(reactor, deviceName, displayName));
		}

		private static void CollectProtoDevices(Vessel v, List<Device> devices)
		{
			var prefabData = new Dictionary<string, Lib.Module_prefab_data>();

			foreach (ProtoPartSnapshot partSnapshot in v.protoVessel.protoPartSnapshots)
			{
				ProtoPartModuleSnapshot engineUpdater = KFFTUtils.TryFindPartModuleSnapshot(partSnapshot, "FFTFusionEngineKerbalismUpdater");
				if (engineUpdater != null)
				{
					string moduleId = Lib.Proto.GetString(engineUpdater, "engineModuleID");
					TryAddProtoDevice(devices, partSnapshot, prefabData, "ModuleFusionEngine", "ModuleID", moduleId, true);
					continue;
				}

				ProtoPartModuleSnapshot reactorUpdater = KFFTUtils.TryFindPartModuleSnapshot(partSnapshot, "FFTFusionReactorKerbalismUpdater");
				if (reactorUpdater == null)
					continue;

				string reactorModuleId = Lib.Proto.GetString(reactorUpdater, "reactorModuleID");
				TryAddProtoDevice(devices, partSnapshot, prefabData, "FusionReactor", "ModuleID", reactorModuleId, false);
			}
		}

		private static void TryAddProtoDevice(
			List<Device> devices,
			ProtoPartSnapshot partSnapshot,
			Dictionary<string, Lib.Module_prefab_data> prefabData,
			string moduleName,
			string idFieldName,
			string moduleId,
			bool isEngine)
		{
			ProtoPartModuleSnapshot moduleSnapshot = FindModuleSnapshot(partSnapshot, moduleName, idFieldName, moduleId);
			if (moduleSnapshot == null)
				return;

			Part partPrefab = PartLoader.getPartInfoByName(partSnapshot.partName).partPrefab;
			prefabData.Clear();
			PartModule modulePrefab = Lib.ModulePrefab(partPrefab.Modules, moduleSnapshot.moduleName, prefabData);
			FusionReactor reactorPrefab = modulePrefab as FusionReactor;
			if (reactorPrefab == null)
				return;

			string resolvedId = Lib.Proto.GetString(moduleSnapshot, idFieldName);
			if (string.IsNullOrEmpty(resolvedId))
				resolvedId = moduleId;

			string deviceName = isEngine
				? "FFT fusion engine reactor " + resolvedId
				: "FFT fusion reactor " + resolvedId;
			string displayName = isEngine
				? Localizer.Format("#LOC_KerbalismFFT_Device_FusionEngineReactor")
				: Localizer.Format("#LOC_KerbalismFFT_Device_FusionReactor");

			devices.Add(new ProtoFusionReactorDevice(reactorPrefab, partSnapshot, moduleSnapshot, deviceName, displayName));
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
