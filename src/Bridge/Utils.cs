using UnityEngine;

namespace KerbalismBridge
{
	public static class BridgeUtils
	{
		public static void Log(string msg)
		{
			Debug.Log("[KerbalismBridge] " + msg);
		}

		public static void LogError(string msg)
		{
			Debug.LogError("[KerbalismBridge] " + msg);
		}

		public static double SampleResourceAbundance(Vessel v, ModuleResourceHarvester harvester)
		{
			// get abundance
			AbundanceRequest request = new AbundanceRequest
			{
				ResourceType = (HarvestTypes) harvester.HarvesterType,
				ResourceName = harvester.ResourceName,
				BodyId = v.mainBody.flightGlobalsIndex,
				Latitude = v.latitude,
				Longitude = v.longitude,
				Altitude = v.altitude,
				CheckForLock = false
			};
			return ResourceMap.Instance.GetAbundance(request);
		}

		// Find PartModule snapshot (used for unloaded vessels as they only have Modules snapshots)
		public static ProtoPartModuleSnapshot FindPartModuleSnapshot(ProtoPartSnapshot p, string PartModuleName)
		{
			ProtoPartModuleSnapshot m = TryFindPartModuleSnapshot(p, PartModuleName);
			if (m == null)
				LogError($" Part [{p.partInfo.title}] No {PartModuleName} was found in part snapshot.");
			return m;
		}

		public static ProtoPartModuleSnapshot TryFindPartModuleSnapshot(ProtoPartSnapshot partSnapshot, string moduleName)
		{
			if (partSnapshot == null)
				return null;

			for (int i = 0; i < partSnapshot.modules.Count; i++)
			{
				if (partSnapshot.modules[i].moduleName == moduleName)
					return partSnapshot.modules[i];
			}

			return null;
		}
	}
}
