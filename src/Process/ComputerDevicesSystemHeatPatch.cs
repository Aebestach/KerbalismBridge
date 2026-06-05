using System.Collections.Generic;
using HarmonyLib;
using KERBALISM;
using KerbalismBridge;

namespace KerbalismProcess
{
	/// <summary>
	/// Kerbalism Automation only switches on moduleName "ProcessController" / "Harvester".
	/// Layer A modules use ProcessControllerSystemHeat / HarvesterSystemHeat, so mirror the
	/// native devices here and remove previous injected devices from Kerbalism's cached list.
	/// </summary>
	[HarmonyPatch(typeof(Computer), nameof(Computer.GetModuleDevices))]
	internal static class Patch_Computer_GetModuleDevices_SystemHeat
	{
		private static void Postfix(Vessel v, ref List<Device> __result)
		{
			if (__result == null || v == null || !Features.Automation)
				return;

			// GetModuleDevices caches the list; Postfix runs on every call, so remove our
			// previous entries before adding the current set.
			RemoveInjectedSystemHeatDevices(__result);

			int insertAt = FindFirstVesselDeviceIndex(__result);
			var added = new List<Device>();

			if (v.loaded)
			{
				foreach (ProcessControllerSystemHeat module in Lib.FindModules<ProcessControllerSystemHeat>(v))
				{
					if (!module.isEnabled)
						continue;

					if (module.resource == "_Nukereactor" && module.toggle)
						added.Add(new FissionReactorProcessDevice(module));
					else if (module.toggle)
						added.Add(new SystemHeatProcessDevice(module));
				}

				foreach (HarvesterSystemHeat module in Lib.FindModules<HarvesterSystemHeat>(v))
				{
					if (!module.isEnabled)
						continue;

					added.Add(new SystemHeatHarvesterDevice(module));
				}
			}
			else
			{
				var prefabData = new Dictionary<string, Lib.Module_prefab_data>();

				foreach (ProtoPartSnapshot protoPart in v.protoVessel.protoPartSnapshots)
				{
					Part partPrefab = PartLoader.getPartInfoByName(protoPart.partName).partPrefab;
					prefabData.Clear();

					foreach (ProtoPartModuleSnapshot protoModule in protoPart.modules)
					{
						PartModule modulePrefab = Lib.ModulePrefab(partPrefab.Modules, protoModule.moduleName, prefabData);
						if (modulePrefab == null || !Lib.Proto.GetBool(protoModule, "isEnabled"))
							continue;

						switch (protoModule.moduleName)
						{
							case "ProcessControllerSystemHeat":
								{
									var prefab = modulePrefab as ProcessControllerSystemHeat;
									if (prefab == null || !prefab.toggle)
										break;
									if (prefab.resource == "_Nukereactor")
										added.Add(new ProtoFissionReactorProcessDevice(prefab, protoPart, protoModule));
									else
										added.Add(new ProtoSystemHeatProcessDevice(prefab, protoPart, protoModule));
									break;
								}
							case "HarvesterSystemHeat":
								added.Add(new ProtoSystemHeatHarvesterDevice(modulePrefab as HarvesterSystemHeat, protoPart, protoModule));
								break;
						}
					}
				}
			}

			if (added.Count > 0)
				__result.InsertRange(insertAt, added);
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

		private static void RemoveInjectedSystemHeatDevices(List<Device> devices)
		{
			for (int i = devices.Count - 1; i >= 0; i--)
			{
				if (devices[i] is SystemHeatProcessDevice
					|| devices[i] is ProtoSystemHeatProcessDevice
					|| devices[i] is FissionReactorProcessDevice
					|| devices[i] is ProtoFissionReactorProcessDevice
					|| devices[i] is SystemHeatHarvesterDevice
					|| devices[i] is ProtoSystemHeatHarvesterDevice)
					devices.RemoveAt(i);
			}
		}
	}
}
